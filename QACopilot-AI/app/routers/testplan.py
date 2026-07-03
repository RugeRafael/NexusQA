from fastapi import APIRouter, HTTPException
from app.models.requests import AnalyzeTestPlanRequest
from app.models.responses import TestPlanAnalysisResponse
from app.services.ai_service import analyze_test_plan
import logging

router = APIRouter()
logger = logging.getLogger(__name__)


@router.post(
    "/api/analyze-testplan",
    response_model=TestPlanAnalysisResponse,
    tags=["Test Plan"]
)
async def analyze_test_plan_endpoint(request: AnalyzeTestPlanRequest):
    try:
        logger.info("Analyzing test plan for project: %s", request.project_name)
        result = await analyze_test_plan(
            plan_content=request.plan_content,
            project_name=request.project_name or ""
        )
        return result
    except Exception as e:
        logger.error("Error analyzing test plan: %s", str(e))
        raise HTTPException(status_code=500, detail=f"Error analyzing test plan: {str(e)}")