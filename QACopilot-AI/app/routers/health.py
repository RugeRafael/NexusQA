from fastapi import APIRouter
from app.models.responses import HealthResponse
from app.config import get_settings

router = APIRouter()
settings = get_settings()


@router.get("/health", response_model=HealthResponse, tags=["Health"])
async def health_check():
    model = settings.claude_model if settings.ai_provider == "claude" else settings.openai_model
    return HealthResponse(
        status="healthy",
        ai_provider=settings.ai_provider,
        model=model
    )