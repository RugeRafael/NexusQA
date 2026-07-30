from app.config import get_settings
from app.services.claude_service import ClaudeService
from app.services.openai_service import OpenAIService
from app.services.rag_service import search_context
from app.prompts.system_prompts import (
    TESTCASE_GENERATION_PROMPT,
    TESTPLAN_ANALYSIS_PROMPT,
    CHAT_QA_PROMPT,
    REPORT_GENERATION_PROMPT,
    BASE_SYSTEM_PROMPT
)
from app.models.responses import (
    TestCaseGenerationResponse,
    TestPlanAnalysisResponse,
    ChatResponse,
    ReportResponse
)
import json
import re
import logging

settings = get_settings()
logger = logging.getLogger(__name__)


def get_ai_client():
    if settings.ai_provider.lower() == "claude":
        return ClaudeService(), f"claude/{settings.claude_model}"
    return OpenAIService(), f"openai/{settings.openai_model}"


def _build_rag_context(query: str, project_id: str = "global") -> str:
    """Obtiene contexto relevante del indice RAG filtrando por proyecto + global."""
    try:
        context = search_context(query, project_id=project_id)
        if context:
            return f"\n\n=== CONTEXTO DE ITHEALTH.CO (Base de conocimiento interna) ===\n{context}\n=== FIN DEL CONTEXTO ===\n\n"
        return ""
    except Exception as e:
        logger.warning("Error obteniendo contexto RAG: %s", e)
        return ""


async def generate_test_cases(
    document_content: str,
    project_name: str = "",
    additional_context: str = "",
    project_id: str = "global"
) -> TestCaseGenerationResponse:
    client, model_name = get_ai_client()
    context = f"Proyecto: {project_name}\n" if project_name else ""
    if additional_context:
        context += f"Contexto adicional: {additional_context}\n"

    rag_context = _build_rag_context(f"casos de prueba {project_name} {document_content[:200]}", project_id=project_id)
    context += rag_context

    prompt = f"{TESTCASE_GENERATION_PROMPT}\n\n{context}{document_content}"
    content, tokens = await client.generate(prompt)

    tc_count = len(re.findall(r'TC-[0-9]+', content))
    if tc_count == 0:
        tc_count = len(re.findall(r'Caso de Prueba [0-9]+', content, re.IGNORECASE))
    if tc_count == 0:
        tc_count = content.lower().count('### caso de prueba')
    if tc_count == 0:
        tc_count = content.lower().count('**id:**')

    confidence = 0.0
    if tc_count > 0:
        confidence = min(0.95, 0.70 + (tc_count * 0.02))
        if 'precondicion' in content.lower():
            confidence = min(0.95, confidence + 0.05)
        if 'resultado esperado' in content.lower():
            confidence = min(0.95, confidence + 0.05)
        if 'istqb' in content.lower():
            confidence = min(0.95, confidence + 0.03)
        if rag_context:
            confidence = min(0.95, confidence + 0.03)

    return TestCaseGenerationResponse(
        content=content,
        total_test_cases=max(tc_count, 1),
        confidence_score=round(confidence, 2),
        model_used=model_name,
        tokens_used=tokens
    )


async def analyze_test_plan(
    plan_content: str,
    project_name: str = ""
) -> TestPlanAnalysisResponse:
    client, model_name = get_ai_client()
    context = f"Proyecto: {project_name}\n" if project_name else ""
    rag_context = _build_rag_context(f"plan de pruebas {project_name}")
    context += rag_context

    prompt = f"{TESTPLAN_ANALYSIS_PROMPT}\n\n{context}{plan_content}"
    content, tokens = await client.generate(prompt)

    content_lower = content.lower()
    not_viable = any(w in content_lower for w in ["no viable", "no factible", "rechazado", "viable: false", "viable: no"])
    is_viable = not not_viable and any(w in content_lower for w in ["viable", "factible", "aprobado", "listo para pruebas"])

    display_content = content
    lines = display_content.split('\n')
    cleaned_lines = [
        line for line in lines
        if not re.search(r'(?i)viable\s*:\s*(true|false)', line)
    ]
    display_content = '\n'.join(cleaned_lines)

    return TestPlanAnalysisResponse(
        is_viable=is_viable,
        viability_reason=_extract_section_numbered(display_content, "2", "RAZON") or display_content[:500],
        istqb_compliance_notes=_extract_istqb(display_content),
        iso29119_compliance_notes=_extract_iso(display_content),
        estimated_time_json=_extract_time_estimation(display_content),
        ai_analysis_result=display_content,
        confidence_score=0.88,
        model_used=model_name
    )


async def chat_with_qa_assistant(
    message: str,
    session_history: list[dict] = [],
    project_id: str = "global",
    project_name: str | None = None
) -> ChatResponse:
    """
    project_name viene resuelto dinamicamente desde la BD (.NET -> ChatService.cs
    consulta la tabla Projects). No hay diccionario hardcodeado aqui.
    Si no llega (ej. llamadas antiguas o project_id="global"), se usa un fallback neutro.
    """
    client, model_name = get_ai_client()

    rag_context = _build_rag_context(message, project_id=project_id)

    # Fallback si por alguna razon no llega el nombre (compatibilidad hacia atras)
    resolved_name = project_name or (project_id if project_id and project_id != "global" else "")

    system_content = BASE_SYSTEM_PROMPT
    if rag_context:
        project_line = f'\nEl proyecto actual se llama "{resolved_name}". ' if resolved_name else ""
        system_content += (
            f"\n\nIMPORTANTE: Tienes acceso al siguiente contexto especifico del proyecto."
            f"{project_line}"
            f" El contexto puede ser tecnico (manuales, configuraciones, interfaces) y puede no mencionar "
            f"el nombre del proyecto textualmente, pero SI pertenece a este proyecto. DEBES usarlo para responder"
            + (f', tratando todo el contenido como perteneciente a "{resolved_name}"' if resolved_name else "")
            + f":\n{rag_context}\n"
            f"NO digas que no tienes informacion sobre el proyecto si el contexto anterior existe. "
            f"Resume y explica ese contenido como informacion del proyecto."
        )

    logger.info(
        "RAG chat -> project_id=%s | project_name=%s | rag_context_len=%d",
        project_id, resolved_name or "N/A", len(rag_context)
    )

    try:
        messages = [{"role": "system", "content": system_content}]

        if session_history:
            recent_history = session_history[-10:] if len(session_history) > 10 else session_history
            for msg in recent_history:
                if msg.get("role") in ["user", "assistant"]:
                    messages.append({"role": msg["role"], "content": msg["content"]})

        messages.append({"role": "user", "content": message})

        content, tokens = await client.generate_with_history(messages)

        return ChatResponse(response=content, model_used=model_name, tokens_used=tokens)
    except Exception as e:
        logger.error("Error in chat: %s", str(e))
        prompt = f"{CHAT_QA_PROMPT}{message}"
        content, tokens = await client.generate(prompt)
        return ChatResponse(response=content, model_used=model_name, tokens_used=tokens)


async def generate_report(
    structure: str,
    instructions: str,
    context: str = ""
) -> ReportResponse:
    client, model_name = get_ai_client()
    rag_context = _build_rag_context(f"informe {structure[:100]}")
    if rag_context:
        context = context + rag_context if context else rag_context

    prompt = REPORT_GENERATION_PROMPT.format(
        structure=structure,
        instructions=instructions,
        context=context or "No se proporciono contexto adicional."
    )
    content, tokens = await client.generate(prompt)
    return ReportResponse(content=content, model_used=model_name, tokens_used=tokens)


def _extract_istqb(content: str) -> str:
    patterns = ["ISTQB", "istqb", "4. ASPECTOS FUERTES", "ASPECTOS FUERTES"]
    for pattern in patterns:
        result = _extract_by_keyword(content, pattern, stop_patterns=["5.", "6.", "ASPECTOS A MEJORAR"])
        if result and len(result) > 50:
            return result
    return _extract_section(content, "ISTQB")


def _extract_iso(content: str) -> str:
    patterns = ["3. CUMPLIMIENTO ISO", "CUMPLIMIENTO ISO 29119", "ISO 29119", "ISO/IEC"]
    for pattern in patterns:
        result = _extract_by_keyword(content, pattern, stop_patterns=["4.", "ASPECTOS FUERTES"])
        if result and len(result) > 50:
            return result
    return _extract_section(content, "ISO")


def _extract_by_keyword(content: str, keyword: str, stop_patterns: list = None) -> str:
    lines = content.split('\n')
    result = []
    capturing = False
    for line in lines:
        if keyword.upper() in line.upper() and not capturing:
            capturing = True
        elif capturing:
            if stop_patterns:
                should_stop = any(line.strip().startswith(s) for s in stop_patterns)
                if should_stop and result:
                    break
        if capturing:
            result.append(line)
        if capturing and len(result) > 15:
            break
    return '\n'.join(result).strip() if result else ""


def _extract_section_numbered(content: str, number: str, keyword: str) -> str:
    lines = content.split('\n')
    result = []
    capturing = False
    for line in lines:
        if (f"{number}." in line or keyword.upper() in line.upper()) and not capturing:
            capturing = True
        elif capturing and re.match(r'^\d+\.', line.strip()) and result:
            break
        if capturing:
            result.append(line)
    return '\n'.join(result[:8]).strip() if result else ""


def _extract_section(content: str, keyword: str) -> str:
    lines = content.split('\n')
    result = []
    capturing = False
    for line in lines:
        if keyword.upper() in line.upper():
            capturing = True
        elif capturing and re.match(r'^\d+\.', line.strip()) and result:
            break
        if capturing:
            result.append(line)
        if capturing and len(result) > 12:
            break
    return '\n'.join(result).strip() if result else f"Ver analisis completo para detalles de {keyword}."


def _extract_time_estimation(content: str) -> str:
    time_data = {}
    patterns = {
        "planificacion": [r"planificaci[o\u00f3]n[^\d]*(\d+)[^\d]*(\d+)[^\d]*(\d+)"],
        "diseno_casos": [r"dise[n\u00f1]o[^\d]*(\d+)[^\d]*(\d+)[^\d]*(\d+)"],
        "preparacion_entorno": [r"preparaci[o\u00f3]n[^\d]*(\d+)[^\d]*(\d+)[^\d]*(\d+)"],
        "ejecucion": [r"ejecuci[o\u00f3]n[^\d]*(\d+)[^\d]*(\d+)[^\d]*(\d+)"],
        "reporte_cierre": [r"reporte[^\d]*(\d+)[^\d]*(\d+)[^\d]*(\d+)"],
    }
    for key, pats in patterns.items():
        for pat in pats:
            match = re.search(pat, content.lower())
            if match:
                opt, _, pes = match.group(1), match.group(2), match.group(3)
                time_data[key] = f"{opt}-{pes} dias"
                break
        if key not in time_data:
            defaults = {
                "planificacion": "3-5 dias", "diseno_casos": "4-6 dias",
                "preparacion_entorno": "2-3 dias", "ejecucion": "8-12 dias",
                "reporte_cierre": "2-3 dias"
            }
            time_data[key] = defaults.get(key, "2-5 dias")

    total_match = re.search(r'optimista[:\s]*(\d+)\s*d[\u00ed\u00ed]as', content.lower())
    time_data["total_optimista"] = f"{total_match.group(1)} dias" if total_match else "19-22 dias"
    total_match = re.search(r'probable[:\s]*(\d+)\s*d[\u00ed\u00ed]as', content.lower())
    time_data["total_probable"] = f"{total_match.group(1)} dias" if total_match else "28-34 dias"
    total_match = re.search(r'pesimista[:\s]*(\d+)\s*d[\u00ed\u00ed]as', content.lower())
    time_data["total_pesimista"] = f"{total_match.group(1)} dias" if total_match else "38-43 dias"

    return json.dumps(time_data, ensure_ascii=False)
