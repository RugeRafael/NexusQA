from fastapi import APIRouter, HTTPException, UploadFile, File, Form
from typing import Optional
import json
import logging
from ..services.report_service import generate_report
from ..services.document_reader import read_document

router = APIRouter(prefix="/api", tags=["reports"])
logger = logging.getLogger(__name__)


@router.post("/generate-report")
async def generate_report_endpoint(
    report_type: str = Form(...),
    project_name: str = Form(...),
    qa_engineer: str = Form(...),
    version: str = Form(default="1.0"),
    period: str = Form(default=""),
    additional_context: str = Form(default=""),
    requirements: str = Form(default="[]"),
    test_cases: str = Form(default="[]"),
    defects: str = Form(default="[]"),
    total_test_cases: int = Form(default=0),
    passed_test_cases: int = Form(default=0),
    failed_test_cases: int = Form(default=0),
    blocked_test_cases: int = Form(default=0),
    total_execution_time: float = Form(default=0),
    jira_bugs: str = Form(default="[]"),
    document: Optional[UploadFile] = File(default=None)
):
    logger.info(f"RAW jira_bugs received: {jira_bugs[:200]}")
    logger.info(f"jira_bugs length: {len(jira_bugs)}")

    # Leer documento
    doc_content = ""
    doc_bytes = b""
    if document and document.filename:
        doc_bytes = await document.read()
        doc_content = read_document("", doc_bytes, document.filename)
        logger.info(f"Document read: {document.filename} - {len(doc_content)} chars")

    # Parsear campos
    try:
        reqs_list = json.loads(requirements)
    except Exception:
        reqs_list = [r.strip() for r in requirements.split('\n') if r.strip()]

    try:
        cases_list = json.loads(test_cases)
    except Exception:
        cases_list = [t.strip() for t in test_cases.split('\n') if t.strip()]

    try:
        defects_list = json.loads(defects)
    except Exception:
        defects_list = [d.strip() for d in defects.split('\n') if d.strip()]

    try:
        jira_bugs_list = json.loads(jira_bugs)
        logger.info(f"jira_bugs parsed OK: {len(jira_bugs_list)} items")
    except Exception as e:
        logger.warning(f"jira_bugs parse error: {e}")
        jira_bugs_list = []

    data = {
        "projectName": project_name,
        "qaEngineer": qa_engineer,
        "version": version,
        "period": period,
        "additionalContext": additional_context,
        "requirements": reqs_list,
        "testCases": cases_list,
        "defects": defects_list,
        "totalTestCases": total_test_cases,
        "passedTestCases": passed_test_cases,
        "failedTestCases": failed_test_cases,
        "blockedTestCases": blocked_test_cases,
        "totalExecutionTimeMinutes": total_execution_time,
        "jiraBugs": jira_bugs_list,
        "documentContent": doc_content,
        "documentBytes": doc_bytes  # bytes raw para parsear el HTML
    }

    result = await generate_report(report_type, data)

    if not result.get("success"):
        raise HTTPException(
            status_code=500,
            detail=result.get("error", "Error generando informe")
        )

    return result
