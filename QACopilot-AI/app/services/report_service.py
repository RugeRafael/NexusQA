import datetime
import re as _re
import logging
from app.config import get_settings
from app.services.claude_service import ClaudeService
from app.services.openai_service import OpenAIService
from app.services.document_reader import parse_test_plan_html

settings = get_settings()
logger = logging.getLogger(__name__)


def get_ai_client():
    if settings.ai_provider.lower() == "claude":
        return ClaudeService()
    return OpenAIService()


COMPLETION_STYLE = """<style>
:root{--bg:#0a0d12;--sur:#111520;--sur2:#161c2d;--bdr:#1e2740;--txt:#e2e8f5;--mut:#7a88ab;--grn:#22c55e;--grn-bg:#0d2d1a;--grn-bdr:#166534;--red:#ef4444;--red-bg:#2d0d0d;--red-bdr:#991b1b;--ylw:#eab308;--ylw-bg:#2d250a;--ylw-bdr:#854d0e;--acc:#6366f1;--hdr-bg:linear-gradient(135deg,#0d1424,#111828,#0d1a2e)}
[data-theme="light"]{--bg:#f4f6fb;--sur:#ffffff;--sur2:#eef0f7;--bdr:#d1d8ef;--txt:#1a1f36;--mut:#64748b;--grn:#16a34a;--grn-bg:#dcfce7;--grn-bdr:#86efac;--red:#dc2626;--red-bg:#fee2e2;--red-bdr:#fca5a5;--ylw:#d97706;--ylw-bg:#fef3c7;--ylw-bdr:#fcd34d;--acc:#4f46e5;--hdr-bg:linear-gradient(135deg,#1e2a4a,#2d3a6b,#1e2a4a)}
.theme-toggle{position:fixed;top:20px;right:24px;z-index:999;background:var(--sur);border:1px solid var(--bdr);border-radius:50px;padding:8px 16px;display:flex;align-items:center;gap:8px;cursor:pointer;font-family:'Space Grotesk',sans-serif;font-size:12px;font-weight:600;color:var(--mut);box-shadow:0 2px 12px rgba(0,0,0,.15)}
.toggle-track{width:34px;height:18px;background:var(--bdr);border-radius:20px;position:relative}.toggle-track.on{background:var(--acc)}
.toggle-thumb{width:12px;height:12px;background:#fff;border-radius:50%;position:absolute;top:3px;left:3px;transition:left .25s}.toggle-track.on .toggle-thumb{left:19px}
*{margin:0;padding:0;box-sizing:border-box}
body{background:var(--bg);color:var(--txt);font-family:'Space Grotesk',sans-serif;min-height:100vh}
header{background:var(--hdr-bg);border-bottom:1px solid var(--bdr);padding:44px 60px 36px;position:relative;overflow:hidden}
.hd-top{display:flex;justify-content:space-between;align-items:flex-start;flex-wrap:wrap;gap:20px}
.tag{font-family:'JetBrains Mono',monospace;font-size:11px;letter-spacing:3px;text-transform:uppercase;color:var(--acc);background:rgba(99,102,241,.1);border:1px solid rgba(99,102,241,.3);padding:5px 13px;border-radius:4px;display:inline-block;margin-bottom:14px}
h1{font-family:'Fraunces',serif;font-size:clamp(26px,4vw,44px);font-weight:700;line-height:1.1;background:linear-gradient(135deg,#e2e8f5,#a5b4fc);-webkit-background-clip:text;-webkit-text-fill-color:transparent;background-clip:text}
.sub{color:var(--mut);font-size:14px;margin-top:8px}.hd-meta{text-align:right}
.hd-meta .sp{font-size:13px;font-weight:600;color:var(--acc);display:block;margin-bottom:4px}
.hd-meta .dt{font-family:'JetBrains Mono',monospace;font-size:11px;color:var(--mut)}
main{padding:40px 60px;max-width:1440px;margin:0 auto}
section{margin-bottom:50px}
.sec-title{font-family:'Fraunces',serif;font-size:20px;font-weight:700;color:var(--txt);margin-bottom:22px;display:flex;align-items:center;gap:12px}
.sec-title::after{content:'';flex:1;height:1px;background:var(--bdr)}
.kpi-grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(165px,1fr));gap:14px}
.kpi{background:var(--sur);border:1px solid var(--bdr);border-radius:12px;padding:22px 18px;position:relative;overflow:hidden;animation:fadeUp .5s ease both}
.kpi.g{border-color:var(--grn-bdr);background:var(--grn-bg)}.kpi.r{border-color:var(--red-bdr);background:var(--red-bg)}.kpi.y{border-color:var(--ylw-bdr);background:var(--ylw-bg)}.kpi.a{border-color:rgba(99,102,241,.3);background:rgba(99,102,241,.06)}
.kpi-lbl{font-size:10px;font-weight:700;letter-spacing:2px;text-transform:uppercase;color:var(--mut);margin-bottom:10px}
.kpi-val{font-family:'Fraunces',serif;font-size:44px;font-weight:700;line-height:1}
.kpi.g .kpi-val{color:var(--grn)}.kpi.r .kpi-val{color:var(--red)}.kpi.y .kpi-val{color:var(--ylw)}.kpi.a .kpi-val{color:#a5b4fc}
.kpi-sub{font-size:11px;color:var(--mut);margin-top:5px}.kpi-bar{position:absolute;bottom:0;left:0;right:0;height:3px}
.kpi.g .kpi-bar{background:var(--grn)}.kpi.r .kpi-bar{background:var(--red)}.kpi.y .kpi-bar{background:var(--ylw)}.kpi.a .kpi-bar{background:var(--acc)}
.search-bar{width:100%;background:var(--sur);border:1px solid var(--bdr);border-radius:10px;padding:11px 16px;color:var(--txt);font-size:13px;outline:none;margin-bottom:14px}
.cases-grid{display:grid;gap:7px}
.card{background:var(--sur);border:1px solid var(--bdr);border-radius:10px;overflow:hidden}
.card.red-c{border-left:4px solid var(--red)}.card.ylw-c{border-left:4px solid var(--ylw)}.card.hidden{display:none}
.card-hd{display:flex;align-items:center;gap:9px;padding:11px 16px;cursor:pointer;flex-wrap:wrap}
.cid{font-family:'JetBrains Mono',monospace;font-size:11px;font-weight:500;padding:3px 8px;border-radius:4px;white-space:nowrap;flex-shrink:0}
.red-c .cid{color:var(--red);background:var(--red-bg);border:1px solid var(--red-bdr)}.ylw-c .cid{color:var(--ylw);background:var(--ylw-bg);border:1px solid var(--ylw-bdr)}
.crf{font-family:'JetBrains Mono',monospace;font-size:10px;color:var(--acc);background:rgba(99,102,241,.1);padding:2px 6px;border-radius:3px;flex-shrink:0}
.cname{flex:1;font-size:13px;font-weight:500;color:var(--txt);min-width:180px}
.ccrit{font-size:10px;font-weight:600;color:var(--mut);background:var(--sur2);padding:2px 8px;border-radius:4px;white-space:nowrap;flex-shrink:0}
.badge{font-size:10px;font-weight:700;padding:3px 9px;border-radius:12px;white-space:nowrap;flex-shrink:0}
.badge-fail{background:var(--red-bg);color:var(--red);border:1px solid var(--red-bdr)}.badge-blk{background:var(--ylw-bg);color:var(--ylw);border:1px solid var(--ylw-bdr)}
.tog{color:var(--mut);font-size:12px;transition:transform .25s;flex-shrink:0}.card.open .tog{transform:rotate(180deg)}
.card-body{display:none;padding:0 16px 14px;border-top:1px solid var(--bdr)}.card.open .card-body{display:block}
.dg{display:grid;grid-template-columns:1fr 1fr;gap:12px;margin-top:12px}
.dlbl{font-size:10px;font-weight:700;letter-spacing:1.5px;text-transform:uppercase;color:var(--mut);margin-bottom:4px}
.dtxt{font-size:12px;color:var(--txt);line-height:1.6;background:var(--sur2);padding:9px 11px;border-radius:6px;border:1px solid var(--bdr)}
.detail-full{grid-column:1/-1;font-size:12px;line-height:1.75;background:rgba(0,0,0,.2);padding:12px 14px;border-radius:6px;margin-top:4px;white-space:pre-wrap}
.detail-full.bug{border:1px solid rgba(239,68,68,.2);color:#fca5a5}
.detail-full.pend{border:1px solid rgba(234,179,8,.2);color:#fde68a}
.conc{background:linear-gradient(135deg,rgba(99,102,241,.08),rgba(139,92,246,.05));border:1px solid rgba(99,102,241,.25);border-radius:14px;padding:30px 34px}
.conc h3{font-family:'Fraunces',serif;font-size:19px;color:var(--acc);margin-bottom:14px}
.conc p{font-size:13px;line-height:1.85;color:var(--txt);margin-bottom:13px}
.pills{display:flex;flex-wrap:wrap;gap:8px;margin-top:18px}
.pill{padding:5px 12px;border-radius:20px;font-size:11px;font-weight:600}
.p-h{background:var(--red-bg);color:var(--red);border:1px solid var(--red-bdr)}.p-m{background:var(--ylw-bg);color:var(--ylw);border:1px solid var(--ylw-bdr)}.p-l{background:var(--grn-bg);color:var(--grn);border:1px solid var(--grn-bdr)}
.mod-wrap{background:var(--sur);border:1px solid var(--bdr);border-radius:12px;overflow:hidden}
.mod-table{width:100%;border-collapse:collapse}
.mod-table th{font-size:10px;font-weight:700;letter-spacing:2px;text-transform:uppercase;color:var(--mut);padding:10px 14px;border-bottom:1px solid var(--bdr);text-align:left;background:var(--sur2)}
.mod-table td{padding:9px 14px;font-size:12px;border-bottom:1px solid var(--bdr);vertical-align:middle}
.rf-pill{font-family:'JetBrains Mono',monospace;font-size:10px;color:var(--acc);background:rgba(99,102,241,.1);border:1px solid rgba(99,102,241,.25);padding:2px 8px;border-radius:3px;display:inline-block}
.num-pass{color:var(--grn);font-weight:700;font-family:'JetBrains Mono',monospace}.num-bug{color:var(--red);font-weight:700;font-family:'JetBrains Mono',monospace}.num-blk{color:var(--ylw);font-weight:700;font-family:'JetBrains Mono',monospace}
.pbar-bg{background:var(--bdr);border-radius:4px;height:7px;overflow:hidden;min-width:80px;display:flex}
.pbar-g{background:var(--grn);height:100%}.pbar-r{background:var(--red);height:100%}
.st-badge{font-size:10px;font-weight:700;padding:3px 9px;border-radius:10px;white-space:nowrap}
.st-ok{background:var(--grn-bg);color:var(--grn);border:1px solid var(--grn-bdr)}.st-bug{background:var(--red-bg);color:var(--red);border:1px solid var(--red-bdr)}.st-mix{background:rgba(99,102,241,.08);color:#a5b4fc;border:1px solid rgba(99,102,241,.25)}
@keyframes fadeUp{from{opacity:0;transform:translateY(14px)}to{opacity:1;transform:translateY(0)}}
</style>
<link href="https://fonts.googleapis.com/css2?family=Space+Grotesk:wght@300;400;500;600;700&family=JetBrains+Mono:wght@400;500&family=Fraunces:ital,wght@0,300;0,700;1,300&display=swap" rel="stylesheet">"""

COMPARISON_STYLE = """<style>
  :root{--navy:#0f2644;--navy-mid:#1a3c5e;--blue:#2e75b6;--blue-lt:#e8f2fb;--ok:#1a7a46;--ok-lt:#e6f5ed;--warn:#b83232;--warn-lt:#fdf0f0;--amber:#c07a00;--amber-lt:#fef8ec;--ink:#1a1a2e;--muted:#5a6a7a;--rule:#d4dde8;--page:#f5f8fc;--white:#ffffff;--mono:'DM Mono',monospace;--serif:'DM Serif Display',serif;--sans:'DM Sans',sans-serif;--radius:6px;--shadow:0 2px 12px rgba(15,38,68,.08)}
  *,*::before,*::after{box-sizing:border-box;margin:0;padding:0}
  body{font-family:var(--sans);background:var(--page);color:var(--ink);font-size:14px;line-height:1.65}
  .report-header{background:var(--navy);color:white;padding:56px 64px 48px;position:relative;overflow:hidden}
  .report-header::after{content:'';position:absolute;bottom:0;left:0;right:0;height:3px;background:linear-gradient(90deg,var(--blue),#5aa0d0,var(--blue))}
  .header-tag{font-family:var(--mono);font-size:11px;letter-spacing:.15em;text-transform:uppercase;color:#7aadcf;margin-bottom:16px}
  .header-title{font-family:var(--serif);font-size:42px;line-height:1.1;margin-bottom:8px}
  .header-subtitle{font-size:17px;color:#9bbdd4;margin-bottom:32px;font-weight:300}
  .header-meta{display:flex;gap:32px;flex-wrap:wrap}
  .meta-label{font-family:var(--mono);font-size:10px;letter-spacing:.12em;text-transform:uppercase;color:#7aadcf}
  .meta-value{font-size:13px;color:#d4e8f5;font-weight:500}
  .print-btn{position:fixed;bottom:28px;right:28px;background:var(--navy);color:white;border:none;border-radius:50px;padding:12px 24px;font-family:var(--sans);font-size:13px;font-weight:600;cursor:pointer;z-index:100}
  .container{max-width:1100px;margin:0 auto;padding:48px}
  .kpi-strip{display:grid;grid-template-columns:repeat(4,1fr);gap:16px;margin-bottom:48px;margin-top:-32px}
  .kpi{background:white;border-radius:var(--radius);padding:20px 24px;box-shadow:var(--shadow);border-top:3px solid transparent}
  .kpi.total{border-top-color:var(--blue)}.kpi.ok{border-top-color:var(--ok)}.kpi.partial{border-top-color:var(--amber)}.kpi.fail{border-top-color:var(--warn)}
  .kpi-num{font-family:var(--serif);font-size:40px;line-height:1;margin-bottom:4px}
  .kpi.total .kpi-num{color:var(--blue)}.kpi.ok .kpi-num{color:var(--ok)}.kpi.partial .kpi-num{color:var(--amber)}.kpi.fail .kpi-num{color:var(--warn)}
  .kpi-label{font-size:12px;color:var(--muted);font-weight:500;text-transform:uppercase;letter-spacing:.06em}
  .card{background:white;border-radius:var(--radius);box-shadow:var(--shadow);margin-bottom:32px;overflow:hidden}
  .card-header{padding:20px 28px;border-bottom:1px solid var(--rule);display:flex;align-items:center;gap:12px}
  .card-num{font-family:var(--mono);font-size:11px;color:var(--blue);background:var(--blue-lt);padding:2px 8px;border-radius:4px}
  .card-title{font-family:var(--serif);font-size:20px;color:var(--navy)}
  .card-body{padding:28px}
  table{width:100%;border-collapse:collapse;font-size:13px}
  thead tr{background:var(--navy)}
  thead th{padding:12px 14px;text-align:left;color:white;font-family:var(--mono);font-size:11px;letter-spacing:.07em;text-transform:uppercase;white-space:nowrap}
  tbody tr{border-bottom:1px solid var(--rule)}
  tbody tr:hover{background:#f0f6fc}
  td{padding:12px 14px;vertical-align:top;line-height:1.5}
  .badge{display:inline-flex;align-items:center;padding:4px 10px;border-radius:50px;font-size:11px;font-weight:600;white-space:nowrap;font-family:var(--mono)}
  .badge-ok{background:var(--ok-lt);color:var(--ok);border:1px solid #a3d9bb}
  .badge-partial{background:var(--amber-lt);color:var(--amber);border:1px solid #f0cc80}
  .badge-fail{background:var(--warn-lt);color:var(--warn);border:1px solid #f0b0b0}
  .req-block{margin-bottom:28px;padding-bottom:28px;border-bottom:1px solid var(--rule)}
  .req-block:last-child{border-bottom:none;margin-bottom:0}
  .req-head{display:flex;align-items:center;gap:10px;margin-bottom:10px}
  .req-id{font-family:var(--mono);font-size:12px;font-weight:700;color:var(--navy);background:var(--blue-lt);padding:3px 10px;border-radius:4px}
  .req-title{font-weight:600;font-size:14.5px;color:var(--navy-mid)}
  .req-body{font-size:13px;color:var(--muted);line-height:1.7}
  .report-footer{text-align:center;padding:24px 48px 48px;font-family:var(--mono);font-size:11px;color:var(--muted);letter-spacing:.06em;text-transform:uppercase}
</style>
<link href="https://fonts.googleapis.com/css2?family=DM+Serif+Display:ital@0;1&family=DM+Mono:wght@400;500&family=DM+Sans:ital,opsz,wght@0,9..40,300;0,9..40,400;0,9..40,500;0,9..40,600&display=swap" rel="stylesheet">"""

INNOVATION_STYLE = """<style>
  :root{--bg:#f7f6f2;--surface:#ffffff;--surface2:#f0efe9;--text:#1a1916;--text-muted:#6b6a65;--text-faint:#a8a79f;--border:#e2e0d8;--accent:#2d5a3d;--accent-light:#e8f0eb;--accent-mid:#4a8c62;--amber:#8a5c1a;--amber-light:#fdf0d8;--red:#8a2020;--red-light:#fde8e8;--blue:#1a3d8a;--blue-light:#e8ecfa;--green-dark:#1a4a2a;--green-light:#e8f5ed;--radius:10px}
  *{box-sizing:border-box;margin:0;padding:0}
  body{font-family:'DM Sans',sans-serif;background:var(--bg);color:var(--text);min-height:100vh;font-size:14px;line-height:1.6}
  .footer{text-align:center;padding:2rem;font-size:11px;color:var(--text-faint);border-top:1px solid var(--border);font-family:'DM Mono',monospace}
</style>
<link href="https://fonts.googleapis.com/css2?family=DM+Sans:wght@300;400;500;600&family=DM+Mono:wght@400;500&display=swap" rel="stylesheet">"""


def _get_rf_for_cp(cp_num: int) -> str:
    if cp_num <= 2: return 'RF-10'
    elif cp_num <= 6: return 'RF-12'
    elif cp_num <= 12: return 'RF-16'
    elif cp_num <= 17: return 'RF-17'
    elif cp_num <= 21: return 'RF-20'
    elif cp_num <= 28: return 'RF-23'
    elif cp_num <= 32: return 'RF-52'
    elif cp_num <= 35: return 'RF-22'
    elif cp_num <= 39: return 'RF-21'
    elif cp_num <= 61: return 'RF-24'
    elif cp_num <= 68: return 'RF-37'
    elif cp_num <= 97: return 'RF-38'
    elif cp_num <= 110: return 'RF-31'
    elif cp_num <= 113: return 'RF-32'
    else: return 'RF-General'


def _cruzar_jira_con_plan(jira_bugs: list, plan_casos: list) -> list:
    enriquecidos = []
    for bug in jira_bugs:
        summary = bug.get('summary', '').lower()
        cp_match = _re.search(r'[\(\s]?(?:CP|TC)-(\d{1,4})[\)\s]?', bug.get('summary', ''))
        cp_id = f"CP-{int(cp_match.group(1)):02d}" if cp_match else ''
        caso = {}
        if cp_id:
            caso = next((c for c in plan_casos if c['id'] == cp_id), {})
        if not caso and plan_casos:
            best_match = None
            best_score = 0
            summary_words = set(summary.split())
            for c in plan_casos:
                nombre_words = set(c.get('nombre', '').lower().split())
                score = len(summary_words & nombre_words)
                if score > best_score and score >= 2:
                    best_score = score
                    best_match = c
            if best_match:
                caso = best_match
                cp_id = caso.get('id', '')
        rf = caso.get('rf', 'RF-General')
        enriquecidos.append({
            **bug,
            'cp_id': cp_id,
            'rf': rf,
            'rf_nombre': caso.get('rf_nombre', ''),
            'nombre_plan': caso.get('nombre', bug.get('summary', '')[:60]),
            'modulo': caso.get('modulo', ''),
            'submodulo': caso.get('submodulo', ''),
            'criticidad': caso.get('criticidad', 'Media'),
        })
    return enriquecidos


def _build_completion_html(project, qa, version, period, fecha,
                           jira_bugs_rich, plan_data, context):
    DONE = ['Finalizada', 'Exitoso', 'Done', 'Finalizado']

    plan_casos_list = plan_data.get('casos', [])
    plan_cps = plan_data.get('total_cps', 0)

    # Mapa de CP -> item Jira
    jira_map_cp = {b.get('cp_id', ''): b for b in jira_bugs_rich if b.get('cp_id')}

    # Calcular KPIs basados en el PLAN, no en Jira
    bugs_activos_cp = []  # CPs del plan con bug activo en Jira
    exitosos_cp = []      # CPs del plan sin bug activo (Exitoso o sin registro)

    for cp in plan_casos_list:
        cp_id = cp.get('id', '')
        jira_item = jira_map_cp.get(cp_id)
        if jira_item:
            jira_status = jira_item.get('status', '')
            if jira_status not in DONE + ['Cancelado']:
                bugs_activos_cp.append(cp_id)
            else:
                exitosos_cp.append(cp_id)
        else:
            exitosos_cp.append(cp_id)  # Sin registro = Exitoso

    # También bugs de Jira sin CP asignado
    bugs_activos_sin_cp = [b for b in jira_bugs_rich if 'Bug' in b.get('issueType', '') and
                           b.get('status') not in DONE + ['Cancelado'] and not b.get('cp_id')]

    cancelados = [b for b in jira_bugs_rich if b.get('status') == 'Cancelado']
    bugs_activos = [b for b in jira_bugs_rich if 'Bug' in b.get('issueType', '') and
                    b.get('status') not in DONE + ['Cancelado']]

    total_j = plan_cps
    pass_j = len(exitosos_cp)
    bug_j = len(bugs_activos_cp) + len(bugs_activos_sin_cp)
    blk_j = len([b for b in jira_bugs_rich if b.get('status') in ['Por hacer', 'En progreso', 'Bloqueado'] and 'Bug' not in b.get('issueType','')])
    cancelados_j = len(cancelados)
    pct_global = round(pass_j / total_j * 100) if total_j > 0 else 0

    plan_rfs = {r['id']: r['nombre'] for r in plan_data.get('rfs', [])}
    rfs_str = ', '.join(list(plan_rfs.keys())[:8]) if plan_rfs else 'RF-General'

    rf_groups = {}
    for b in jira_bugs_rich:
        rf = b.get('rf', 'RF-General')
        rf_nombre = b.get('rf_nombre', '') or plan_rfs.get(rf, rf)
        if rf not in rf_groups:
            rf_groups[rf] = {'nombre': rf_nombre, 'bugs': []}
        rf_groups[rf]['bugs'].append(b)

    header_html = (
        f"<button class='theme-toggle' onclick='toggleTheme()' id='themeBtn'>"
        f"<span id='themeIcon'>&#9728;</span>"
        f"<div class='toggle-track' id='toggleTrack'><div class='toggle-thumb'></div></div>"
        f"<span id='themeLabel' style='font-size:11px'>Claro</span></button>"
        f"<header><div class='hd-top'>"
        f"<div><div class='tag'>NEXUSQA &mdash; ITHEALTH</div>"
        f"<h1>{project}</h1>"
        f"<p class='sub'>{qa} &middot; {period}</p></div>"
        f"<div class='hd-meta'>"
        f"<span class='sp'>Informe de Entrega</span>"
        f"<span class='dt'>{fecha}</span>"
        f"<span class='dt'>Version {version}</span>"
        f"</div></div></header>"
        f"<main>"
    )

    kpi_html = (
        f"<section><div class='kpi-grid'>"
        f"<div class='kpi a'><div class='kpi-lbl'>CPs en Plan</div><div class='kpi-val'>{plan_cps}</div><div class='kpi-sub'>Casos definidos</div><div class='kpi-bar'></div></div>"
        f"<div class='kpi g'><div class='kpi-lbl'>Finalizados</div><div class='kpi-val'>{pass_j}</div><div class='kpi-sub'>{pct_global}% del total</div><div class='kpi-bar'></div></div>"
        f"<div class='kpi r'><div class='kpi-lbl'>Bugs Activos</div><div class='kpi-val'>{bug_j}</div><div class='kpi-sub'>Sin resolver</div><div class='kpi-bar'></div></div>"
        f"<div class='kpi y'><div class='kpi-lbl'>Pendientes</div><div class='kpi-val'>{blk_j}</div><div class='kpi-sub'>Por ejecutar</div><div class='kpi-bar'></div></div>"
        f"</div></section>"
    )

    rows_modulos = ""
    for rf, group in sorted(rf_groups.items()):
        bugs = group['bugs']
        rf_exit = len([b for b in bugs if b.get('status') in DONE])
        rf_bug = len([b for b in bugs if 'Bug' in b.get('issueType', '') and b.get('status') not in DONE + ['Cancelado']])
        rf_blk = len([b for b in bugs if b.get('status') in ['Por hacer', 'Bloqueado', 'En progreso']])
        rf_total = len(bugs)
        rf_pct = round(rf_exit / rf_total * 100) if rf_total > 0 else 0
        rf_cl = 'st-ok' if rf_pct >= 80 else 'st-mix' if rf_pct >= 50 else 'st-bug'
        rf_nombre = group['nombre'] or plan_rfs.get(rf, rf)
        rows_modulos += (
            f"<tr>"
            f"<td><span class='rf-pill'>{rf}</span></td>"
            f"<td>{rf_nombre}</td>"
            f"<td>{rf_total}</td>"
            f"<td><span class='num-pass'>{rf_exit}</span></td>"
            f"<td><span class='num-bug'>{rf_bug}</span></td>"
            f"<td><span class='num-blk'>{rf_blk}</span></td>"
            f"<td><div class='pbar-bg'><div class='pbar-g' style='width:{rf_pct}%'></div>"
            f"<div class='pbar-r' style='width:{100-rf_pct}%'></div></div></td>"
            f"<td><span class='st-badge {rf_cl}'>{rf_pct}%</span></td>"
            f"</tr>"
        )

    seccion_modulos = (
        f"<section>"
        f"<div class='sec-title'>Estado por RF / Modulo</div>"
        f"<div class='mod-wrap'>"
        f"<table class='mod-table'><thead><tr>"
        f"<th>RF</th><th>Modulo</th><th>Total</th><th>Exitosos</th><th>Bugs</th><th>Pendientes</th><th>Progreso</th><th>Estado</th>"
        f"</tr></thead><tbody>{rows_modulos}</tbody></table></div></section>"
    )

    cards_html = ""
    for b in bugs_activos:
        status = b.get('status', '')
        key = b.get('key', '')
        summary = b.get('summary', '')
        nombre_plan = b.get('nombre_plan', summary)
        url = b.get('url', '')
        issue_type = b.get('issueType', '')
        rf = b.get('rf', '')
        modulo = b.get('modulo', '')
        submodulo = b.get('submodulo', '')
        criticidad = b.get('criticidad', 'Media')
        cp_id = b.get('cp_id', '')

        if status == 'Por hacer':
            card_class = "card ylw-c"
            badge = "<span class='badge badge-blk'>Por hacer</span>"
            data_s = "yellow"
            detail_class = "detail-full pend"
        else:
            card_class = "card red-c"
            badge = "<span class='badge badge-fail'>Bug Activo</span>"
            data_s = "red"
            detail_class = "detail-full bug"

        display_name = f"{cp_id} &mdash; {nombre_plan[:65]}" if cp_id else nombre_plan[:70]
        cards_html += (
            f"<div class='{card_class}' data-s='{data_s}' "
            f"data-q='{key.lower()} {cp_id.lower()} {summary.lower()[:50]}'>"
            f"<div class='card-hd' onclick='tc(this)'>"
            f"<span class='cid'>{key}</span>"
            f"<span class='crf'>{rf}</span>"
            f"<span class='cname'>{display_name}</span>"
            f"<span class='ccrit'>{status}</span>"
            f"{badge}"
            f"<span class='tog'>&#9660;</span>"
            f"</div>"
            f"<div class='card-body'><div class='dg'>"
            f"<div><div class='dlbl'>CP en Plan</div><div class='dtxt'>{cp_id or 'Sin CP asignado'}</div></div>"
            f"<div><div class='dlbl'>Modulo</div><div class='dtxt'>{modulo}</div></div>"
            f"<div><div class='dlbl'>Sub-Modulo</div><div class='dtxt'>{submodulo}</div></div>"
            f"<div><div class='dlbl'>Criticidad</div><div class='dtxt'>{criticidad}</div></div>"
            f"<div class='{detail_class}'>"
            f"Nombre en plan: {nombre_plan}\n"
            f"Resumen Jira: {summary}\n\n"
            f"&#128279; <a href='{url}' target='_blank' style='color:#6366f1'>{url}</a>"
            f"</div></div></div></div>"
        )

    if not cards_html:
        cards_html = "<div style='text-align:center;padding:40px;color:var(--grn)'>&#10003; Sin bugs activos pendientes</div>"

    # TABLA CPs DEL PLAN vs JIRA
    jira_map = {b.get('cp_id', ''): b for b in jira_bugs_rich if b.get('cp_id')}
    jira_by_key = {b.get('key', ''): b for b in jira_bugs_rich}

    rows_plan = ""
    plan_casos = plan_data.get('casos', [])
    for cp in plan_casos:
        cp_id = cp.get('id', '')
        cp_nombre = cp.get('nombre', '')[:60]
        cp_rf = cp.get('rf', '')
        cp_modulo = cp.get('modulo', '')
        cp_submodulo = cp.get('submodulo', '')
        cp_crit = cp.get('criticidad', 'Media')
        crit_class = 'crit-alta' if cp_crit == 'Alta' else 'crit-baja' if cp_crit == 'Baja' else 'crit-media'

        # Buscar en Jira
        jira_item = jira_map.get(cp_id, {})
        jira_status = jira_item.get('status', '') if jira_item else ''
        jira_key = jira_item.get('key', '') if jira_item else ''
        jira_url = jira_item.get('url', '') if jira_item else ''

        if not jira_status:
            # Sin registro en Jira = Exitoso
            estado_badge = "<span style='color:var(--grn);font-weight:700;font-size:11px'>&#10003; Exitoso</span>"
            key_html = "<span style='color:var(--mut);font-size:11px'>-</span>"
        elif jira_status in DONE:
            estado_badge = f"<span style='color:var(--grn);font-weight:700;font-size:11px'>&#10003; {jira_status}</span>"
            key_html = f"<a href='{jira_url}' target='_blank' style='color:var(--grn);font-family:monospace;font-size:11px'>{jira_key}</a>" if jira_key else "<span style='color:var(--mut);font-size:11px'>-</span>"
        else:
            estado_badge = f"<span style='color:var(--red);font-weight:700;font-size:11px'>&#9888; {jira_status}</span>"
            key_html = f"<a href='{jira_url}' target='_blank' style='color:var(--red);font-family:monospace;font-size:11px'>{jira_key}</a>" if jira_key else "<span style='color:var(--mut);font-size:11px'>-</span>"

        rows_plan += (
            f"<tr>"
            f"<td><span class='cp-id' style='font-family:monospace;font-size:11px;color:var(--acc);background:rgba(99,102,241,.1);border:1px solid rgba(99,102,241,.3);padding:2px 8px;border-radius:3px'>{cp_id}</span></td>"
            f"<td style='font-size:12px'>{cp_nombre}</td>"
            f"<td><span style='font-family:monospace;font-size:10px;color:var(--acc)'>{cp_rf}</span></td>"
            f"<td style='font-size:11px'>{cp_modulo}</td>"
            f"<td style='font-size:11px'>{cp_submodulo}</td>"
            f"<td><span class='{crit_class}' style='font-size:11px;font-weight:600'>{cp_crit}</span></td>"
            f"<td>{key_html}</td>"
            f"<td>{estado_badge}</td>"
            f"</tr>"
        )

    seccion_plan = (
        f"<section>"
        f"<div class='sec-title'>Casos de Prueba del Plan ({len(plan_casos)})</div>"
        f"<div class='mod-wrap'>"
        f"<table class='mod-table'><thead><tr>"
        f"<th>CP</th><th>Nombre</th><th>RF</th><th>Modulo</th><th>Sub-Modulo</th><th>Criticidad</th><th>Key Jira</th><th>Estado</th>"
        f"</tr></thead><tbody>{rows_plan}</tbody></table></div></section>"
    )

    seccion_bugs = (
        f"<section>"
        f"<div class='sec-title'>Bugs Activos ({bug_j})</div>"
        f"<input class='search-bar' id='sb' placeholder='Buscar por key, CP, nombre...' oninput='applyF()'>"
        f"<div class='cases-grid' id='cg'>{cards_html}</div>"
        f"</section>"
    )

    aval = "SE DA AVAL" if pct_global >= 80 else "NO SE DA AVAL"
    aval_color = "var(--grn)" if pct_global >= 80 else "var(--red)"
    check_icon = "&#10003;" if pct_global >= 80 else "&#9888;"
    bugs_txt = "No se registraron bugs criticos." if bug_j == 0 else "Los bugs activos requieren atencion."
    context_html = f"<p>{context}</p>" if context else ""

    bugs_pill = "<span class='pill p-h'>Resolver bugs activos</span>" if bug_j > 0 else "<span class='pill p-l'>Sin bugs activos</span>"
    conclusion_html = (
        f"<section><div class='conc'>"
        f"<h3>Conclusion &mdash; {project}</h3>"
        f"<p>El plan de <strong>{plan_cps} casos de prueba</strong> fue ejecutado contra "
        f"<strong>{total_j} items de Jira</strong>. "
        f"Se finalizaron <strong>{pass_j} items ({pct_global}%)</strong>. "
        f"Quedan <strong>{bug_j} bugs activos</strong> sin resolver. {bugs_txt}</p>"
        f"<p style='font-size:15px;font-weight:700;color:{aval_color};margin-top:8px'>"
        f"{check_icon} {aval} A DESPLIEGUE</p>"
        f"{context_html}"
        f"<div class='pills'>"
        f"{bugs_pill}"
        f"<span class='pill p-l'>Cobertura {pct_global}%</span>"
        f"<span class='pill p-l'>RFs: {rfs_str[:40]}</span>"
        f"</div></div></section>"
    )

    script_html = (
        "<script>"
        "function tc(hd){hd.closest('.card').classList.toggle('open');}"
        "function applyF(){var q=document.getElementById('sb')?document.getElementById('sb').value.toLowerCase():'';document.querySelectorAll('.card').forEach(function(c){var mq=!q||(c.dataset.q||'').includes(q);c.classList.toggle('hidden',!mq);});}"
        "function toggleTheme(){var d=document.documentElement.hasAttribute('data-theme');document.documentElement[d?'removeAttribute':'setAttribute']('data-theme','light');document.getElementById('toggleTrack').classList.toggle('on',!d);document.getElementById('themeIcon').textContent=d?'&#9728;':'&#9790;';document.getElementById('themeLabel').textContent=d?'Claro':'Oscuro';}"
        "</script>"
    )

    return (
        header_html + kpi_html + seccion_modulos + seccion_plan + seccion_bugs + conclusion_html + "</main>" + script_html
    )


async def generate_completion_report(data: dict) -> str:
    project = data.get('projectName', 'Proyecto')
    qa = data.get('qaEngineer', 'QA Engineer')
    version = data.get('version', '1.0')
    period = data.get('period', '')
    context = data.get('additionalContext', '')
    doc_bytes = data.get('documentBytes', b'')
    jira_bugs = data.get('jiraBugs', [])
    fecha = datetime.datetime.now().strftime('%d/%m/%Y')

    logger.info(f"DOC_CONTENT length: {len(data.get('documentContent', ''))}")
    logger.info(f"JIRA_BUGS count: {len(jira_bugs)}")

    plan_data = {"proyecto": project, "qa": qa, "rfs": [], "casos": [], "total_cps": 0}
    if doc_bytes:
        plan_data = parse_test_plan_html(doc_bytes)
        logger.info(f"Plan parseado: {plan_data['total_cps']} CPs, {len(plan_data['rfs'])} RFs")

    jira_bugs_rich = _cruzar_jira_con_plan(jira_bugs, plan_data.get('casos', []))
    logger.info(f"Bugs enriquecidos: {len(jira_bugs_rich)}")

    return _build_completion_html(project, qa, version, period, fecha, jira_bugs_rich, plan_data, context)


async def generate_comparison_report(data: dict) -> str:
    project = data.get('projectName', 'Proyecto')
    qa = data.get('qaEngineer', 'QA Engineer')
    version = data.get('version', '1.0')
    period = data.get('period', '')
    requirements = data.get('requirements', [])
    test_cases = data.get('testCases', [])
    context = data.get('additionalContext', '')
    doc_content = data.get('documentContent', '')
    fecha = datetime.datetime.now().strftime('%d/%m/%Y')

    reqs_text = '\n'.join([f"- {r}" for r in requirements]) if requirements else 'Ver documento adjunto'
    cases_text = '\n'.join([f"- {t}" for t in test_cases]) if test_cases else 'Ver documento adjunto'
    doc_summary = f"DOCUMENTO:\n{doc_content[:3000]}\n\n" if doc_content else ""

    prompt = (
        "Eres un experto QA senior de ithealth.co.\n"
        "Genera un informe HTML profesional de Comparacion Requerimientos vs Plan de Pruebas.\n"
        "GENERA SOLO el contenido del body (sin DOCTYPE/html/head/body/style).\n\n"
        f"Proyecto: {project} | QA: {qa} | Version: {version} | Periodo: {period} | Fecha: {fecha}\n\n"
        f"REQUERIMIENTOS:\n{reqs_text}\n\n"
        f"CASOS DE PRUEBA:\n{cases_text}\n\n"
        f"{doc_summary}"
        f"Contexto: {context}\n\n"
        "Estructura: print-btn, header.report-header, div.container con kpi-strip, "
        "secciones numeradas 01-07, report-footer.\n"
        "NO markdown, NO ```html. Genera HTML real ahora:"
    )

    client = get_ai_client()
    response, _ = await client.generate(prompt)
    return response


async def generate_innovation_report(data: dict) -> str:
    project = data.get('projectName', 'Proyecto')
    qa = data.get('qaEngineer', 'QA Engineer')
    version = data.get('version', '1.0')
    period = data.get('period', '')
    context = data.get('additionalContext', '')
    doc_content = data.get('documentContent', '')
    fecha = datetime.datetime.now().strftime('%d/%m/%Y')

    prompt = (
        "Eres un experto QA senior de ithealth.co.\n"
        "Genera un informe HTML profesional de Innovacion y Mejoras QA.\n"
        "GENERA SOLO el contenido del body (sin DOCTYPE/html/head/body/style).\n\n"
        f"Proyecto: {project} | QA: {qa} | Version: {version} | Periodo: {period} | Fecha: {fecha}\n\n"
        f"Innovacion descrita:\n{context}\n\n"
        f"Documento adjunto:\n{doc_content[:2000] if doc_content else 'No adjuntado'}\n\n"
        "NO markdown, NO ```html. Genera HTML real ahora:"
    )

    client = get_ai_client()
    response, _ = await client.generate(prompt)
    return response


async def generate_report(report_type: str, data: dict) -> dict:
    try:
        if report_type == 'completion':
            body_html = await generate_completion_report(data)
            style = COMPLETION_STYLE
            title = "Informe de Entrega - Casos de Prueba"
        elif report_type == 'comparison':
            body_html = await generate_comparison_report(data)
            style = COMPARISON_STYLE
            title = "Informe - Requerimientos vs. Plan de Pruebas"
        elif report_type == 'innovation':
            body_html = await generate_innovation_report(data)
            style = INNOVATION_STYLE
            title = f"Informe de Innovacion - {data.get('projectName', '')}"
        else:
            return {"success": False, "error": f"Tipo desconocido: {report_type}"}

        body_html = body_html.strip()
        for prefix in ['```html', '```']:
            if body_html.startswith(prefix):
                body_html = body_html[len(prefix):]
        if body_html.endswith('```'):
            body_html = body_html[:-3]
        body_html = body_html.strip()

        full_html = (
            "<!DOCTYPE html>\n<html lang='es'>\n<head>\n"
            "<meta charset='UTF-8'>\n"
            "<meta name='viewport' content='width=device-width, initial-scale=1.0'>\n"
            f"<title>{title}</title>\n"
            f"{style}\n"
            "</head>\n<body>\n"
            f"{body_html}\n"
            "</body>\n</html>"
        )

        return {"success": True, "htmlContent": full_html, "title": title}

    except Exception as e:
        logger.error(f"Error generating report: {e}")
        return {"success": False, "error": str(e)}
