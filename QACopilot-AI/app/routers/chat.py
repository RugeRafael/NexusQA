from fastapi import APIRouter, HTTPException
from app.models.requests import ChatRequest
from app.models.responses import ChatResponse
from app.services.ai_service import chat_with_qa_assistant
import logging

router = APIRouter()
logger = logging.getLogger(__name__)


@router.post(
    "/api/chat",
    response_model=ChatResponse,
    tags=["Chat QA Assistant"]
)
async def chat_endpoint(request: ChatRequest):
    try:
        logger.info("Chat request received: %s...", request.message[:50])
        result = await chat_with_qa_assistant(
            message=request.message,
            session_history=request.session_history or []
        )
        return result
    except Exception as e:
        logger.error("Error in chat: %s", str(e))
        raise HTTPException(status_code=500, detail=f"Error in chat: {str(e)}")