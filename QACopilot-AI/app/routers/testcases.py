from fastapi import APIRouter, HTTPException
from app.models.requests import GenerateTestCasesRequest
from app.models.responses import TestCaseGenerationResponse
from app.services.ai_service import generate_test_cases
import logging

router = APIRouter()
logger = logging.getLogger(__name__)


@router.post(
    "/api/generate-testcases",
    response_model=TestCaseGenerationResponse,
    tags=["Test Cases"]
)
async def generate_test_cases_endpoint(request: GenerateTestCasesRequest):
    try:
        logger.info("Generating test cases for content length: %d", len(request.document_content))
        result = await generate_test_cases(
            document_content=request.document_content,
            project_name=request.project_name or "",
            additional_context=request.additional_context or ""
        )
        logger.info("Generated %d test cases", result.total_test_cases)
        return result
    except Exception as e:
        logger.error("Error generating test cases: %s", str(e))
        raise HTTPException(status_code=500, detail=f"Error generating test cases: {str(e)}")