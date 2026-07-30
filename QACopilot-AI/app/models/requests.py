from pydantic import BaseModel, Field
from typing import Optional


class GenerateTestCasesRequest(BaseModel):
    document_content: str = Field(..., min_length=10, description="Contenido del requerimiento")
    project_name: Optional[str] = Field(None, description="Nombre del proyecto")
    additional_context: Optional[str] = Field(None, description="Contexto adicional")
    project_id: Optional[str] = Field("global", description="ID del proyecto para filtrar RAG")


class AnalyzeTestPlanRequest(BaseModel):
    plan_content: str = Field(..., min_length=10, description="Contenido del plan de pruebas")
    project_name: Optional[str] = Field(None, description="Nombre del proyecto")


class ChatRequest(BaseModel):
    message: str = Field(..., min_length=1, description="Mensaje del usuario")
    session_history: Optional[list[dict]] = Field(default=[], description="Historial de la conversacion")
    project_id: Optional[str] = Field("global", description="ID del proyecto para filtrar RAG")
    project_name: Optional[str] = Field(None, description="Nombre real del proyecto (resuelto desde la BD)")


class GenerateReportRequest(BaseModel):
    structure: str = Field(..., description="Estructura JSON del informe")
    instructions: str = Field(..., description="Instrucciones del Senior")
    context: Optional[str] = Field("", description="Contexto del proyecto")
