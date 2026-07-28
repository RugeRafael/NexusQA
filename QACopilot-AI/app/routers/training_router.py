"""
Endpoints Python para gestión RAG de documentos de entrenamiento.
El backend .NET llama estos endpoints al subir/eliminar documentos.
"""
from fastapi import APIRouter, HTTPException
from pydantic import BaseModel
from app.services.rag_service import index_document, delete_document, get_index_stats
import logging

router = APIRouter(prefix="/training", tags=["training"])
logger = logging.getLogger(__name__)


class IndexRequest(BaseModel):
    doc_id: str
    file_path: str
    category: str = "general"


class DeleteRequest(BaseModel):
    doc_id: str


@router.post("/index")
async def index_doc(request: IndexRequest):
    """Vectoriza e indexa un documento en ChromaDB."""
    success = index_document(request.doc_id, request.file_path, request.category)
    if not success:
        raise HTTPException(status_code=500, detail="Error indexando documento")
    return {"success": True, "message": f"Documento {request.doc_id} indexado correctamente"}


@router.delete("/index/{doc_id}")
async def delete_doc(doc_id: str):
    """Elimina un documento del índice vectorial."""
    success = delete_document(doc_id)
    if not success:
        raise HTTPException(status_code=500, detail="Error eliminando documento del índice")
    return {"success": True, "message": f"Documento {doc_id} eliminado del índice"}


@router.get("/stats")
async def get_stats():
    """Estadísticas del índice RAG."""
    return get_index_stats()
