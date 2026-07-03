from pydantic import BaseModel
from typing import Optional


class TestCaseGenerationResponse(BaseModel):
    content: str
    total_test_cases: int
    confidence_score: float
    model_used: str
    tokens_used: Optional[int] = None


class TestPlanAnalysisResponse(BaseModel):
    is_viable: bool
    viability_reason: str
    istqb_compliance_notes: str
    iso29119_compliance_notes: str
    estimated_time_json: str
    ai_analysis_result: str
    confidence_score: float
    model_used: str


class ChatResponse(BaseModel):
    response: str
    model_used: str
    tokens_used: Optional[int] = None


class ReportResponse(BaseModel):
    content: str
    model_used: str
    tokens_used: Optional[int] = None


class HealthResponse(BaseModel):
    status: str
    ai_provider: str
    model: str
    version: str = "1.0.0"