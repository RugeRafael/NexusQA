"""
RAG Service - Retrieval Augmented Generation para NexusQA
Usa ChromaDB como base vectorial local y sentence-transformers para embeddings.
Los documentos nunca salen del servidor local.
"""
import os
import logging
from pathlib import Path
from typing import Optional

logger = logging.getLogger(__name__)

# Base de datos vectorial — almacenada localmente
CHROMA_DB_PATH = os.environ.get("CHROMA_DB_PATH", "C:/NexusQA/QACopilot/uploads/training/chroma_db")
COLLECTION_NAME = "ithealth_training_docs"
EMBEDDING_MODEL = "paraphrase-multilingual-MiniLM-L12-v2"  # Modelo multilingüe liviano
MAX_CONTEXT_CHARS = 8000  # Límite de contexto para el prompt
TOP_K_RESULTS = 5  # Fragmentos más relevantes a recuperar

_client = None
_collection = None
_embedder = None


def _get_client():
    global _client, _collection
    if _client is None:
        try:
            import chromadb
            _client = chromadb.PersistentClient(path=CHROMA_DB_PATH)
            _collection = _client.get_or_create_collection(
                name=COLLECTION_NAME,
                metadata={"hnsw:space": "cosine"}
            )
            logger.info("ChromaDB inicializado en: %s", CHROMA_DB_PATH)
        except Exception as e:
            logger.error("Error inicializando ChromaDB: %s", e)
            _client = None
    return _client, _collection


def _get_embedder():
    global _embedder
    if _embedder is None:
        try:
            from sentence_transformers import SentenceTransformer
            _embedder = SentenceTransformer(EMBEDDING_MODEL)
            logger.info("Embedder cargado: %s", EMBEDDING_MODEL)
        except Exception as e:
            logger.error("Error cargando embedder: %s", e)
    return _embedder


def _extract_text(file_path: str) -> str:
    """Extrae texto de PDF, DOCX o TXT."""
    ext = Path(file_path).suffix.lower()
    try:
        if ext == ".pdf":
            from pypdf import PdfReader
            reader = PdfReader(file_path)
            return "\n".join(p.extract_text() or "" for p in reader.pages)
        elif ext in [".docx", ".doc"]:
            from docx import Document
            doc = Document(file_path)
            return "\n".join(p.text for p in doc.paragraphs)
        elif ext in [".txt", ".md", ".html"]:
            with open(file_path, "r", encoding="utf-8", errors="ignore") as f:
                return f.read()
        else:
            logger.warning("Extensión no soportada para extracción: %s", ext)
            return ""
    except Exception as e:
        logger.error("Error extrayendo texto de %s: %s", file_path, e)
        return ""


def _chunk_text(text: str, chunk_size: int = 500, overlap: int = 50) -> list[str]:
    """Divide el texto en fragmentos con overlap para mejor recuperación."""
    words = text.split()
    chunks = []
    for i in range(0, len(words), chunk_size - overlap):
        chunk = " ".join(words[i:i + chunk_size])
        if chunk.strip():
            chunks.append(chunk)
    return chunks


def index_document(doc_id: str, file_path: str, category: str = "general") -> bool:
    """
    Vectoriza e indexa un documento en ChromaDB.
    Se llama cuando el Admin sube un documento de entrenamiento.
    """
    client, collection = _get_client()
    if not client or not collection:
        logger.error("ChromaDB no disponible para indexar")
        return False

    embedder = _get_embedder()
    if not embedder:
        logger.error("Embedder no disponible para indexar")
        return False

    try:
        # Extraer texto
        text = _extract_text(file_path)
        if not text.strip():
            logger.warning("Documento vacío o sin texto extraíble: %s", file_path)
            return False

        # Dividir en fragmentos
        chunks = _chunk_text(text)
        if not chunks:
            return False

        # Eliminar chunks previos del mismo documento
        try:
            existing = collection.get(where={"doc_id": doc_id})
            if existing["ids"]:
                collection.delete(ids=existing["ids"])
        except Exception:
            pass

        # Generar embeddings y almacenar
        embeddings = embedder.encode(chunks, show_progress_bar=False).tolist()
        ids = [f"{doc_id}_chunk_{i}" for i in range(len(chunks))]
        metadatas = [{"doc_id": doc_id, "category": category, "chunk_index": i} for i in range(len(chunks))]

        collection.add(
            ids=ids,
            embeddings=embeddings,
            documents=chunks,
            metadatas=metadatas
        )

        logger.info("Documento indexado: %s — %d fragmentos", doc_id, len(chunks))
        return True

    except Exception as e:
        logger.error("Error indexando documento %s: %s", doc_id, e)
        return False


def delete_document(doc_id: str) -> bool:
    """Elimina un documento del índice vectorial."""
    client, collection = _get_client()
    if not client or not collection:
        return False
    try:
        existing = collection.get(where={"doc_id": doc_id})
        if existing["ids"]:
            collection.delete(ids=existing["ids"])
            logger.info("Documento eliminado del índice: %s", doc_id)
        return True
    except Exception as e:
        logger.error("Error eliminando documento %s: %s", doc_id, e)
        return False


def search_context(query: str, project_id: Optional[str] = None, top_k: int = TOP_K_RESULTS) -> str:
    """
    Busca los fragmentos más relevantes para una consulta.
    Combina documentos del proyecto específico + documentos globales.
    """
    client, collection = _get_client()
    if not client or not collection:
        logger.warning("ChromaDB no disponible — sin contexto RAG")
        return ""

    embedder = _get_embedder()
    if not embedder:
        return ""

    try:
        count = collection.count()
        if count == 0:
            logger.info("No hay documentos en el índice RAG")
            return ""

        query_embedding = embedder.encode([query], show_progress_bar=False).tolist()
        context_parts = []
        total_chars = 0
        seen_chunks = set()

        def fetch_results(where_filter):
            try:
                res = collection.query(
                    query_embeddings=query_embedding,
                    n_results=min(top_k, count),
                    where=where_filter,
                    include=["documents", "distances"]
                )
                return res
            except Exception:
                return None

        # 1. Buscar en documentos del proyecto específico
        if project_id and project_id != "global":
            results = fetch_results({"category": project_id})
            if results and results["documents"] and results["documents"][0]:
                for doc, distance in zip(results["documents"][0], results["distances"][0]):
                    if distance < 0.85 and doc not in seen_chunks:
                        if total_chars + len(doc) <= MAX_CONTEXT_CHARS // 2:
                            context_parts.append(doc)
                            total_chars += len(doc)
                            seen_chunks.add(doc)

        # 2. Complementar con documentos globales
        results = fetch_results({"category": "global"})
        if results and results["documents"] and results["documents"][0]:
            for doc, distance in zip(results["documents"][0], results["distances"][0]):
                if distance < 0.85 and doc not in seen_chunks:
                    if total_chars + len(doc) <= MAX_CONTEXT_CHARS:
                        context_parts.append(doc)
                        total_chars += len(doc)
                        seen_chunks.add(doc)

        if not context_parts:
            return ""

        context = "\n\n---\n\n".join(context_parts)
        logger.info("RAG: %d fragmentos recuperados (%d chars) para proyecto %s",
                    len(context_parts), total_chars, project_id or "global")
        return context

    except Exception as e:
        logger.error("Error en búsqueda RAG: %s", e)
        return ""


def get_index_stats() -> dict:
    """Retorna estadísticas del índice vectorial."""
    client, collection = _get_client()
    if not client or not collection:
        return {"status": "unavailable", "total_chunks": 0}
    try:
        return {
            "status": "ok",
            "total_chunks": collection.count(),
            "db_path": CHROMA_DB_PATH
        }
    except Exception as e:
        return {"status": "error", "error": str(e), "total_chunks": 0}
