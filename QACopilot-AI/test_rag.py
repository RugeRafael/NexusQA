from app.services.rag_service import _get_client, _get_embedder
c, col = _get_client()
embedder = _get_embedder()
q = embedder.encode(['Live-Lis enrutador manual'], show_progress_bar=False).tolist()
r = col.query(query_embeddings=q, n_results=3, where={'category': '7c51e979-96c7-4f5d-8961-3d0d5020eb68'}, include=['documents','distances'])
print('Distancias:', r['distances'])
print('Docs:', [d[:100] for d in r['documents'][0]])
