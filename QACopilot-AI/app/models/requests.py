from pydantic import BaseModel, Field
from typing import Optional


class GenerateTestCasesRequest(BaseModel):
    document_content: str = Field(..., min_length=10, description="Contenido del requerimiento")
    project_name: Optional[str] = Field(None, description="Nombre del proyecto")
    additional_context: Optional[str] = Field(None, description="Contexto adicional")


class AnalyzeTestPlanRequest(BaseModel):
    plan_content: str = Field(..., min_length=10, description="Contenido del plan de pruebas")
    project_name: Optional[str] = Field(None, description="Nombre del proyecto")


class ChatRequest(BaseModel):
    message: str = Field(..., min_length=1, description="Mensaje del usuario")
    session_history: Optional[list[dict]] = Field(
        default=[], description="Historial de la conversación")


class GenerateReportRequest(BaseModel):
    structure: str = Field(..., description="Estructura JSON del informe")
    instructions: str = Field(..., description="Instrucciones del Senior")
    context: Optional[str] = Field("", description="Contexto del proyecto")