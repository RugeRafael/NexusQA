import logging
from fastapi import FastAPI, Request
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import JSONResponse
from app.config import get_settings
from app.routers import health, testcases, testplan, chat, reports
from app.routers.training_router import router as training_router

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s - %(name)s - %(levelname)s - %(message)s"
)
logger = logging.getLogger(__name__)
settings = get_settings()

app = FastAPI(
    title="NexusQA — AI Microservice",
    description="Microservicio de IA con RAG para generación de casos de prueba, análisis de planes y chatbot QA basado en ISTQB e ISO 29119.",
    version="2.0.0",
    docs_url="/docs",
    redoc_url="/redoc"
)

allowed_origins = settings.allowed_origins.split(",")
app.add_middleware(
    CORSMiddleware,
    allow_origins=allowed_origins,
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

@app.middleware("http")
async def log_requests(request: Request, call_next):
    logger.info("Request: %s %s", request.method, request.url.path)
    response = await call_next(request)
    logger.info("Response: %s %s -> %d", request.method, request.url.path, response.status_code)
    return response

@app.exception_handler(Exception)
async def global_exception_handler(request: Request, exc: Exception):
    logger.error("Unhandled exception: %s", str(exc))
    return JSONResponse(
        status_code=500,
        content={"detail": "Internal server error", "error": str(exc)}
    )

app.include_router(health.router)
app.include_router(testcases.router)
app.include_router(testplan.router)
app.include_router(chat.router)
app.include_router(reports.router)
app.include_router(training_router)

@app.on_event("startup")
async def startup_event():
    logger.info("NexusQA AI Microservice starting...")
    logger.info("AI Provider: %s", settings.ai_provider)
    model = settings.claude_model if settings.ai_provider == "claude" else settings.openai_model
    logger.info("Model: %s", model)
    logger.info("RAG: ChromaDB habilitado")
    logger.info("Ready to receive requests on port %d", settings.port)
