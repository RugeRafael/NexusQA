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
.charts-row{display:grid;grid-template-columns:1fr 1.5fr 1fr;gap:18px}
.ch-card{background:var(--sur);border:1px solid var(--bdr);border-radius:12px;padding:22px}
.ch-title{font-size:11px;font-weight:700;color:var(--mut);text-transform:uppercase;letter-spacing:1.5px;margin-bottom:16px}
.ch-wrap{position:relative;height:200px}
.pass-table-wrap{background:var(--sur);border:1px solid var(--grn-bdr);border-radius:12px;overflow:hidden}
.pass-table-header{background:var(--grn-bg);padding:14px 20px}
.pass-table-header span{font-size:13px;font-weight:600;color:var(--grn)}
.pass-table{width:100%;border-collapse:collapse}
.pass-table th{font-size:10px;font-weight:700;letter-spacing:2px;text-transform:uppercase;color:var(--mut);padding:10px 16px;border-bottom:1px solid var(--bdr);text-align:left}
.pass-table td{padding:9px 16px;font-size:12px;border-bottom:1px solid var(--bdr)}
.cp-id{font-family:'JetBrains Mono',monospace;font-size:11px;color:var(--grn);background:var(--grn-bg);border:1px solid var(--grn-bdr);padding:2px 8px;border-radius:3px;white-space:nowrap}
.rf-tag{font-family:'JetBrains Mono',monospace;font-size:10px;color:var(--acc);background:rgba(99,102,241,.1);padding:2px 7px;border-radius:3px}
.crit-alta{color:#f87171;font-size:11px;font-weight:600}.crit-media{color:#fbbf24;font-size:11px;font-weight:600}.crit-baja{color:var(--mut);font-size:11px;font-weight:600}
.search-bar{width:100%;background:var(--sur);border:1px solid var(--bdr);border-radius:10px;padding:11px 16px;color:var(--txt);font-size:13px;outline:none;margin-bottom:14px}
.filter-bar{display:flex;gap:8px;margin-bottom:18px;flex-wrap:wrap}
.fbtn{padding:6px 14px;border-radius:7px;border:1px solid var(--bdr);background:var(--sur);color:var(--mut);font-size:12px;font-weight:600;cursor:pointer}
.fbtn.active{background:var(--acc);border-color:var(--acc);color:#fff}.fbtn.r.active{background:var(--red);border-color:var(--red)}.fbtn.y.active{background:var(--ylw);border-color:var(--ylw);color:#000}
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
.badge-ok{background:var(--grn-bg);color:var(--grn);border:1px solid var(--grn-bdr)}
.tog{color:var(--mut);font-size:12px;transition:transform .25s;flex-shrink:0}.card.open .tog{transform:rotate(180deg)}
.card-body{display:none;padding:0 16px 14px;border-top:1px solid var(--bdr)}.card.open .card-body{display:block}
.dg{display:grid;grid-template-columns:1fr 1fr;gap:12px;margin-top:12px}
.dlbl{font-size:10px;font-weight:700;letter-spacing:1.5px;text-transform:uppercase;color:var(--mut);margin-bottom:4px}
.dtxt{font-size:12px;color:var(--txt);line-height:1.6;background:var(--sur2);padding:9px 11px;border-radius:6px;border:1px solid var(--bdr)}
.detail-full{grid-column:1/-1;font-size:12px;line-height:1.75;background:rgba(0,0,0,.2);padding:12px 14px;border-radius:6px;margin-top:4px;white-space:pre-wrap}
.detail-full.ok{border:1px solid rgba(34,197,94,.2);color:#86efac}
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
<link href="https://fonts.googleapis.com/css2?family=Space+Grotesk:wght@300;400;500;600;700&family=JetBrains+Mono:wght@400;500&family=Fraunces:ital,wght@0,300;0,700;1,300&display=swap" rel="stylesheet">
<script src="https://cdnjs.cloudflare.com/ajax/libs/Chart.js/4.4.0/chart.umd.min.js"></script>"""

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
  [data-theme="dark"]{--bg:#111210;--surface:#1c1d1a;--surface2:#242520;--text:#e8e6df;--text-muted:#8a8980;--text-faint:#565650;--border:#2e2f2a;--accent:#5aad74;--accent-light:#1a2e1f;--accent-mid:#4a9060;--amber:#d4953a;--amber-light:#2a1f0a;--red:#d46060;--red-light:#2a1010;--blue:#6a8cd4;--blue-light:#0e1628;--green-dark:#5aad74;--green-light:#0e2015}
  *{box-sizing:border-box;margin:0;padding:0}
  body{font-family:'DM Sans',sans-serif;background:var(--bg);color:var(--text);min-height:100vh;font-size:14px;line-height:1.6}
  .header{background:var(--surface);border-bottom:1px solid var(--border);padding:0 2rem;display:flex;align-items:center;justify-content:space-between;height:56px}
  .theme-toggle{background:var(--surface2);border:1px solid var(--border);border-radius:20px;padding:5px 12px;font-size:12px;color:var(--text-muted);cursor:pointer;font-family:'DM Sans',sans-serif}
  .filter-bar{background:var(--surface);border-bottom:1px solid var(--border);padding:10px 2rem;display:flex;align-items:center;gap:8px;flex-wrap:wrap}
  .filter-btn{background:var(--surface2);border:1px solid var(--border);border-radius:20px;padding:4px 12px;font-size:12px;font-weight:500;color:var(--text-muted);cursor:pointer;font-family:'DM Sans',sans-serif}
  .filter-btn.active{background:var(--text);border-color:var(--text);color:var(--bg)}
  .main{max-width:900px;margin:0 auto;padding:2rem 1.5rem}
  .kpi-row{display:grid;grid-template-columns:repeat(4,1fr);gap:12px;margin-bottom:2rem}
  .kpi{background:var(--surface);border:1px solid var(--border);border-radius:var(--radius);padding:1rem 1.25rem;position:relative;overflow:hidden}
  .kpi::before{content:'';position:absolute;top:0;left:0;right:0;height:2px;background:var(--accent)}
  .kpi:nth-child(2)::before{background:var(--amber)}.kpi:nth-child(3)::before{background:var(--green-dark)}.kpi:nth-child(4)::before{background:var(--blue)}
  .kpi-label{font-size:11px;font-weight:500;color:var(--text-faint);text-transform:uppercase;letter-spacing:.06em;margin-bottom:6px}
  .kpi-value{font-size:28px;font-weight:300;color:var(--text);font-family:'DM Mono',monospace;line-height:1}
  .section{margin-bottom:2rem}
  .section-header{display:flex;align-items:center;gap:10px;margin-bottom:1rem}
  .section-title{font-size:16px;font-weight:600;color:var(--text)}
  .cards{display:flex;flex-direction:column;gap:8px}
  .card{background:var(--surface);border:1px solid var(--border);border-radius:var(--radius);overflow:hidden}
  .card.hidden{display:none}
  .card-trigger{display:flex;align-items:center;gap:12px;padding:14px 16px;cursor:pointer}
  .card-trigger:hover{background:var(--surface2)}
  .card-id{font-family:'DM Mono',monospace;font-size:11px;font-weight:500;color:var(--text-faint);padding:3px 8px;background:var(--surface2);border:1px solid var(--border);border-radius:6px}
  .card-title{font-size:13.5px;font-weight:500;color:var(--text);flex:1}
  .badges{display:flex;gap:5px;align-items:center}
  .badge{font-size:10.5px;font-weight:500;padding:3px 9px;border-radius:20px;white-space:nowrap}
  .p-alta{background:var(--red-light);color:var(--red)}.p-media{background:var(--amber-light);color:var(--amber)}
  .s-finalizado{background:var(--green-light);color:var(--green-dark)}.s-qa{background:var(--blue-light);color:var(--blue)}
  .chevron{width:16px;height:16px;color:var(--text-faint);transition:transform .2s}
  .card.open .chevron{transform:rotate(180deg)}
  .card-body{display:none;padding:0 16px 16px;border-top:1px solid var(--border)}
  .card.open .card-body{display:block}
  .body-grid{display:grid;grid-template-columns:1fr 1fr;gap:12px;margin-top:14px}
  .body-block{background:var(--surface2);border:1px solid var(--border);border-radius:var(--radius);padding:12px 14px}
  .body-block-label{font-size:10px;font-weight:600;color:var(--text-faint);text-transform:uppercase;letter-spacing:.08em;margin-bottom:6px}
  .body-block-text{font-size:13px;color:var(--text-muted);line-height:1.65}
  .body-block.entregable{border-color:var(--accent-mid);background:var(--accent-light)}
  .body-block.entregable .body-block-label{color:var(--accent-mid)}
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
    plan_map = {c['id']: c for c in plan_casos}
    enriquecidos = []
    for bug in jira_bugs:
        summary = bug.get('summary', '')
        cp_match = _re.search(r'CP-(\d{3})', summary)
        cp_num = int(cp_match.group(1)) if cp_match else 0
        cp_id = f"CP-{cp_num:03d}" if cp_num > 0 else ''
        caso = plan_map.get(cp_id, {})
        rf = caso.get('rf', _get_rf_for_cp(cp_num) if cp_num > 0 else 'RF-General')
        enriquecidos.append({
            **bug,
            'cp_id': cp_id,
            'cp_num': cp_num,
            'rf': rf,
            'rf_nombre': caso.get('rf_nombre', ''),
            'nombre_plan': caso.get('nombre', summary[:60]),
            'modulo': caso.get('modulo', 'ValiQC QCE'),
            'criticidad': caso.get('criticidad', 'Media'),
        })
    return enriquecidos


def _build_kpi_section(total_j, pass_j, bug_j, blk_j, pct_global, plan_cps):
    return (
        f"<section><div class='kpi-grid'>"
        f"<div class='kpi a'><div class='kpi-lbl'>Total Items</div><div class='kpi-val'>{total_j}</div><div class='kpi-sub'>Jira subtareas</div><div class='kpi-bar'></div></div>"
        f"<div class='kpi g'><div class='kpi-lbl'>Finalizados</div><div class='kpi-val'>{pass_j}</div><div class='kpi-sub'>{pct_global}% del total</div><div class='kpi-bar'></div></div>"
        f"<div class='kpi r'><div class='kpi-lbl'>Bugs Activos</div><div class='kpi-val'>{bug_j}</div><div class='kpi-sub'>Sin resolver</div><div class='kpi-bar'></div></div>"
        f"<div class='kpi y'><div class='kpi-lbl'>Pendientes</div><div class='kpi-val'>{blk_j}</div><div class='kpi-sub'>Por ejecutar</div><div class='kpi-bar'></div></div>"
        f"<div class='kpi a'><div class='kpi-lbl'>Plan Total</div><div class='kpi-val'>{plan_cps}</div><div class='kpi-sub'>CPs definidos</div><div class='kpi-bar'></div></div>"
        f"</div></section>"
    )


def _build_charts_section(pass_j, bug_j, blk_j, cancelados_j, rf_groups):
    rf_items = list(rf_groups.items())[:8]
    rf_labels = ','.join(["'" + rf + "'" for rf, _ in rf_items])
    rf_data_fin = ','.join([
        str(len([b for b in g['bugs'] if b.get('status') in ['Finalizada','Exitoso','Done','Finalizado']]))
        for _, g in rf_items
    ])
    rf_data_total = ','.join([str(len(g['bugs'])) for _, g in rf_items])
    return (
        "<section><div class='charts-row'>"
        "<div class='ch-card'>"
        "<div class='ch-title'>Distribucion Estado</div>"
        "<div class='ch-wrap' style='position:relative;height:180px;display:flex;align-items:center;justify-content:center;'>"
        "<canvas id='c1'></canvas>"
        "</div></div>"
        "<div class='ch-card'>"
        "<div class='ch-title'>Finalizados por RF</div>"
        "<div class='ch-wrap' style='position:relative;height:180px;'>"
        "<canvas id='c2'></canvas>"
        "</div></div>"
        "<div class='ch-card'>"
        "<div class='ch-title'>Criticidad del Plan</div>"
        "<div class='ch-wrap' style='position:relative;height:180px;'>"
        "<canvas id='c3'></canvas>"
        "</div></div>"
        "</div></section>",
        {
            'pass_j': pass_j, 'bug_j': bug_j, 'blk_j': blk_j, 'cancelados_j': cancelados_j,
            'rf_labels': rf_labels, 'rf_data_fin': rf_data_fin, 'rf_data_total': rf_data_total
        }
    )


def _build_conclusion_section(project, pass_j, total_j, pct_global, bug_j, blk_j, cancelados_j, rfs_str, context):
    aval = "SE DA AVAL" if pct_global >= 80 else "NO SE DA AVAL"
    aval_color = "var(--grn)" if pct_global >= 80 else "var(--red)"
    check_icon = "&#10003;" if pct_global >= 80 else "&#9888;"
    no_bugs = "No se registraron bugs criticos que impidan el despliegue."
    si_bugs = "Los bugs activos requieren atencion antes del despliegue."
    bugs_txt = no_bugs if bug_j == 0 else si_bugs
    cancel_txt = "Los items cancelados (" + str(cancelados_j) + ") fueron excluidos del alcance." if cancelados_j > 0 else ""
    context_html = "<p>" + context + "</p>" if context else ""
    no_bugs_pill = "<span class='pill p-l'>Sin bugs activos</span>"
    si_bugs_pill = "<span class='pill p-h'>Resolver bugs activos</span>"
    bugs_pill = si_bugs_pill if bug_j > 0 else no_bugs_pill
    pend_pill = "<span class='pill p-m'>Ejecutar items pendientes (" + str(blk_j) + ")</span>" if blk_j > 0 else ""
    rfs_short = rfs_str[:40]
    pct_str = str(pct_global)
    html = "<section><div class='conc'>"
    html += "<h3>Conclusion del Ciclo &mdash; " + project + "</h3>"
    html += "<p>El ciclo de pruebas ejecutado sobre el plan formal de <strong>" + str(total_j)
    html += " items</strong> registrados en Jira arrojo un total de <strong>" + str(pass_j)
    html += " items finalizados</strong>, representando el <strong>" + pct_str
    html += "%</strong> de cobertura. Los requerimientos funcionales cubiertos incluyen: " + rfs_str + ".</p>"
    html += "<p>Se identificaron <strong>" + str(bug_j) + " bugs activos</strong> sin resolver y "
    html += "<strong>" + str(blk_j) + " items pendientes</strong> de ejecucion. "
    html += bugs_txt + " " + cancel_txt + "</p>"
    html += "<p style='font-size:15px;font-weight:700;color:" + aval_color + ";margin-top:8px'>"
    html += check_icon + " " + aval + " A DESPLIEGUE EN AMBIENTE DEMO</p>"
    html += context_html
    html += "<div class='pills'>" + bugs_pill + pend_pill
    html += "<span class='pill p-l'>Cobertura " + pct_str + "% alcanzada</span>"
    html += "<span class='pill p-l'>RFs: " + rfs_short + "</span>"
    html += "</div></div></section>"
    return html


def _build_script_section(chart_data=None):
    charts_js = ""
    if chart_data:
        p = chart_data['pass_j']
        b = chart_data['bug_j']
        bl = chart_data['blk_j']
        c = chart_data['cancelados_j']
        rl = chart_data['rf_labels']
        rf = chart_data['rf_data_fin']
        rt = chart_data['rf_data_total']
        total = p + b + bl + c
        charts_js = (
            "var isDark=!document.documentElement.hasAttribute('data-theme');"
            "var gridColor=isDark?'rgba(255,255,255,0.08)':'rgba(0,0,0,0.06)';"
            "var tickColor=isDark?'#7a88ab':'#64748b';"
            "var c1el=document.getElementById('c1');"
            "var c2el=document.getElementById('c2');"
            "var c3el=document.getElementById('c3');"
            "if(c1el){"
            "new Chart(c1el,{"
            "type:'doughnut',"
            "data:{"
            "labels:['Finalizados','Bugs activos','Pendientes','Cancelados'],"
            "datasets:[{"
            "data:[" + str(p) + "," + str(b) + "," + str(bl) + "," + str(c) + "],"
            "backgroundColor:['#22c55e','#ef4444','#f59e0b','#6366f1'],"
            "borderColor:isDark?'#111520':'#ffffff',"
            "borderWidth:3,"
            "hoverOffset:8"
            "}]"
            "},"
            "options:{"
            "responsive:true,"
            "maintainAspectRatio:false,"
            "layout:{padding:10},"
            "plugins:{"
            "legend:{"
            "position:'bottom',"
            "labels:{color:tickColor,font:{size:11,family:'Space Grotesk'},padding:12,usePointStyle:true,pointStyleWidth:8}"
            "},"
            "tooltip:{"
            "callbacks:{"
            "label:function(ctx){"
            "var pct=total>0?Math.round(ctx.parsed/" + str(total) + "*100):0;"
            "return ctx.label+': '+ctx.parsed+' ('+pct+'%)';"
            "}"
            "}"
            "}"
            "},"
            "cutout:'62%'"
            "}"
            "});}"
            "if(c2el){"
            "new Chart(c2el,{"
            "type:'bar',"
            "data:{"
            "labels:[" + rl + "],"
            "datasets:["
            "{label:'Finalizados',data:[" + rf + "],backgroundColor:'rgba(34,197,94,0.85)',borderRadius:4,borderSkipped:false},"
            "{label:'Total',data:[" + rt + "],backgroundColor:isDark?'rgba(99,102,241,0.25)':'rgba(99,102,241,0.15)',borderRadius:4,borderSkipped:false}"
            "]"
            "},"
            "options:{"
            "responsive:true,"
            "maintainAspectRatio:false,"
            "plugins:{legend:{labels:{color:tickColor,font:{size:10,family:'Space Grotesk'},usePointStyle:true}}},"
            "scales:{"
            "x:{ticks:{color:tickColor,font:{size:9}},grid:{display:false},border:{display:false}},"
            "y:{ticks:{color:tickColor,font:{size:9},stepSize:1},grid:{color:gridColor},border:{display:false},beginAtZero:true}"
            "}"
            "}"
            "});}"
            "if(c3el){"
            "new Chart(c3el,{"
            "type:'doughnut',"
            "data:{"
            "labels:['Alta','Media','Baja'],"
            "datasets:[{"
            "data:[0," + str(p+b+bl+c) + ",0],"
            "backgroundColor:['#ef4444','#f59e0b','#22c55e'],"
            "borderColor:isDark?'#111520':'#ffffff',"
            "borderWidth:3,"
            "hoverOffset:8"
            "}]"
            "},"
            "options:{"
            "responsive:true,"
            "maintainAspectRatio:false,"
            "layout:{padding:10},"
            "plugins:{"
            "legend:{position:'bottom',labels:{color:tickColor,font:{size:11,family:'Space Grotesk'},padding:12,usePointStyle:true,pointStyleWidth:8}}"
            "},"
            "cutout:'55%'"
            "}"
            "});}"
        )
    return (
        "<script>"
        "function tc(hd){hd.closest('.card').classList.toggle('open');}"
        "let cur='all';"
        "function setF(f,btn){cur=f;document.querySelectorAll('.fbtn').forEach(function(b){b.classList.remove('active')});btn.classList.add('active');applyF();}"
        "function applyF(){var q=document.getElementById('sb')?document.getElementById('sb').value.toLowerCase():'';document.querySelectorAll('.card').forEach(function(c){var mf=cur==='all'||c.dataset.s===cur;var mq=!q||(c.dataset.q||'').includes(q);c.classList.toggle('hidden',!(mf&&mq));});}"
        "function toggleTheme(){var d=document.documentElement.hasAttribute('data-theme');document.documentElement[d?'removeAttribute':'setAttribute']('data-theme','light');document.getElementById('toggleTrack').classList.toggle('on',!d);document.getElementById('themeIcon').textContent=d?'&#9728;':'&#9790;';document.getElementById('themeLabel').textContent=d?'Claro':'Oscuro';}"
        "window.addEventListener('load',function(){"
        + charts_js +
        "});"
        "</script>"
    )



def _build_completion_html(project, qa, version, period, fecha,
                           jira_bugs_rich, plan_data, context):
    exitosos = [b for b in jira_bugs_rich if b.get('status') in ['Finalizada','Exitoso','Done','Finalizado']]
    cancelados = [b for b in jira_bugs_rich if b.get('status') == 'Cancelado']
    pendientes = [b for b in jira_bugs_rich if b.get('status') in ['Por hacer','En progreso','Bloqueado']]
    bugs_activos = [b for b in jira_bugs_rich if 'Bug' in b.get('issueType','') and
                    b.get('status') not in ['Finalizada','Exitoso','Done','Finalizado','Cancelado']]

    total_j = len(jira_bugs_rich)
    pass_j = len(exitosos)
    bug_j = len(bugs_activos)
    blk_j = len(pendientes)
    cancelados_j = len(cancelados)
    pct_global = round(pass_j / total_j * 100) if total_j > 0 else 0
    plan_cps = plan_data.get('total_cps', 0)

    # RFs del plan
    plan_rfs = {r['id']: r['nombre'] for r in plan_data.get('rfs', [])}
    rfs_str = ', '.join(list(plan_rfs.keys())[:8]) if plan_rfs else 'RF-10, RF-12, RF-17, RF-20, RF-23'

    # Agrupar por RF
    rf_groups = {}
    for b in jira_bugs_rich:
        rf = b.get('rf', 'RF-General')
        rf_nombre = b.get('rf_nombre', '') or plan_rfs.get(rf, rf)
        if rf not in rf_groups:
            rf_groups[rf] = {'nombre': rf_nombre, 'bugs': []}
        rf_groups[rf]['bugs'].append(b)

    # HEADER - construido en Python directamente
    header_html = (
        f"<button class='theme-toggle' onclick='toggleTheme()' id='themeBtn'>"
        f"<span id='themeIcon'>☀️</span>"
        f"<div class='toggle-track' id='toggleTrack'><div class='toggle-thumb'></div></div>"
        f"<span id='themeLabel' style='font-size:11px'>Claro</span></button>"
        f"<header><div class='hd-top'>"
        f"<div><div class='tag'>QA COPILOT &mdash; ITHEALTH</div>"
        f"<h1>{project}</h1>"
        f"<p class='sub'>{qa} &middot; {period}</p></div>"
        f"<div class='hd-meta'>"
        f"<span class='sp'>Plan de Pruebas Finalizado</span>"
        f"<span class='dt'>{fecha}</span>"
        f"<span class='dt'>Version {version}</span>"
        f"</div></div></header>"
        f"<main>"
    )

    # KPIs - construidos en Python
    kpi_html = _build_kpi_section(total_j, pass_j, bug_j, blk_j, pct_global, plan_cps)

    # Charts - construidos en Python
    charts_html, chart_data = _build_charts_section(pass_j, bug_j, blk_j, cancelados_j, rf_groups)

    # TABLA EXITOSOS
    rows_exitosos = ""
    for b in exitosos[:25]:
        cp_id = b.get('cp_id', '')
        rf = b.get('rf', '')
        nombre = b.get('nombre_plan', b.get('summary',''))[:65]
        modulo = b.get('modulo', project)
        criticidad = b.get('criticidad', 'Media')
        crit_class = 'crit-alta' if criticidad == 'Alta' else 'crit-baja' if criticidad == 'Baja' else 'crit-media'
        key = b.get('key', '')
        url = b.get('url', '')
        rows_exitosos += (
            f"<tr>"
            f"<td><span class='cp-id'>{cp_id or key}</span></td>"
            f"<td><a href='{url}' target='_blank' style='color:var(--acc);font-family:monospace;font-size:11px;text-decoration:none'>{key}</a></td>"
            f"<td><span class='rf-tag'>{rf}</span></td>"
            f"<td>{nombre}</td>"
            f"<td>{modulo}</td>"
            f"<td><span class='{crit_class}'>{criticidad}</span></td>"
            f"</tr>"
        )
    if not rows_exitosos:
        rows_exitosos = "<tr><td colspan='6' style='text-align:center;color:var(--mut);padding:20px'>No hay casos exitosos</td></tr>"

    seccion_exitosos = (
        f"<section>"
        f"<div class='sec-title'>Casos Exitosos &mdash; {pass_j} de {total_j}</div>"
        f"<div class='pass-table-wrap'>"
        f"<div class='pass-table-header'><span>&#10003; Casos finalizados correctamente</span></div>"
        f"<table class='pass-table'><thead><tr>"
        f"<th>CP Plan</th><th>Key Jira</th><th>RF</th><th>Nombre en Plan</th><th>Modulo</th><th>Criticidad</th>"
        f"</tr></thead><tbody>{rows_exitosos}</tbody></table></div></section>"
    )

    # TABLA MODULOS
    rows_modulos = ""
    for rf, group in sorted(rf_groups.items()):
        bugs = group['bugs']
        rf_exit = len([b for b in bugs if b.get('status') in ['Finalizada','Exitoso','Done','Finalizado']])
        rf_bug = len([b for b in bugs if 'Bug' in b.get('issueType','') and
                      b.get('status') not in ['Finalizada','Exitoso','Done','Finalizado','Cancelado']])
        rf_blk = len([b for b in bugs if b.get('status') in ['Por hacer','Bloqueado','En progreso']])
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

    # CARDS TODOS LOS ITEMS
    cards_html = ""
    for b in jira_bugs_rich:
        status = b.get('status', '')
        key = b.get('key', '')
        summary = b.get('summary', '')
        nombre_plan = b.get('nombre_plan', summary)
        url = b.get('url', '')
        issue_type = b.get('issueType', '')
        rf = b.get('rf', '')
        modulo = b.get('modulo', '')
        criticidad = b.get('criticidad', 'Media')
        cp_id = b.get('cp_id', '')

        if status in ['Finalizada','Exitoso','Done','Finalizado']:
            card_class = "card"
            badge = "<span class='badge badge-ok'>&#10003; Finalizado</span>"
            data_s = "green"
            detail_class = "detail-full ok"
        elif status == 'Cancelado':
            card_class = "card ylw-c"
            badge = "<span class='badge badge-blk'>Cancelado</span>"
            data_s = "yellow"
            detail_class = "detail-full pend"
        elif status == 'Por hacer':
            card_class = "card ylw-c"
            badge = "<span class='badge badge-blk'>Por hacer</span>"
            data_s = "yellow"
            detail_class = "detail-full pend"
        else:
            card_class = "card red-c"
            badge = "<span class='badge badge-fail'>Bug</span>"
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
            f"<div><div class='dlbl'>Tipo Jira</div><div class='dtxt'>{issue_type}</div></div>"
            f"<div><div class='dlbl'>Criticidad Plan</div><div class='dtxt'>{criticidad}</div></div>"
            f"<div class='{detail_class}'>"
            f"Nombre en plan: {nombre_plan}\n"
            f"Resumen Jira: {summary}\n\n"
            f"&#128279; <a href='{url}' target='_blank' style='color:#6366f1'>{url}</a>"
            f"</div></div></div></div>"
        )

    seccion_items = (
        f"<section>"
        f"<div class='sec-title'>Todos los Items Jira ({total_j})</div>"
        f"<input class='search-bar' id='sb' placeholder='Buscar por key, CP, nombre...' oninput='applyF()'>"
        f"<div class='filter-bar'>"
        f"<button class='fbtn active' onclick='setF(\"all\",this)'>Todos ({total_j})</button>"
        f"<button class='fbtn' style='color:var(--grn)' onclick='setF(\"green\",this)'>Finalizados ({pass_j})</button>"
        f"<button class='fbtn r' onclick='setF(\"red\",this)'>Bugs ({bug_j})</button>"
        f"<button class='fbtn y' onclick='setF(\"yellow\",this)'>Pendientes/Cancelados ({blk_j+cancelados_j})</button>"
        f"</div>"
        f"<div class='cases-grid' id='cg'>{cards_html}</div>"
        f"</section>"
    )

    # CONCLUSION - construida en Python
    conclusion_html = _build_conclusion_section(
        project, pass_j, total_j, pct_global, bug_j, blk_j, cancelados_j, rfs_str, context
    )

    # SCRIPT - construido en Python
    script_html = _build_script_section(chart_data)

    # ENSAMBLAR TODO
    return (
        header_html
        + kpi_html
        + charts_html
        + seccion_exitosos
        + seccion_modulos
        + seccion_items
        + conclusion_html
        + "</main>"
        + script_html
    )


async def generate_completion_report(data: dict) -> str:
    project = data.get('projectName', 'Proyecto')
    qa = data.get('qaEngineer', 'QA Engineer')
    version = data.get('version', '1.0')
    period = data.get('period', '')
    context = data.get('additionalContext', '')
    doc_content = data.get('documentContent', '')
    doc_bytes = data.get('documentBytes', b'')
    jira_bugs = data.get('jiraBugs', [])
    fecha = datetime.datetime.now().strftime('%d/%m/%Y')

    logger.info(f"DOC_CONTENT length: {len(doc_content)}")
    logger.info(f"JIRA_BUGS count: {len(jira_bugs)}")

    # Parsear el plan de pruebas
    plan_data = {"proyecto": project, "qa": qa, "rfs": [], "casos": [], "total_cps": 0}
    if doc_bytes:
        plan_data = parse_test_plan_html(doc_bytes)
        logger.info(f"Plan parseado: {plan_data['total_cps']} CPs, {len(plan_data['rfs'])} RFs")

    # Cruzar bugs con el plan
    jira_bugs_rich = _cruzar_jira_con_plan(jira_bugs, plan_data.get('casos', []))
    logger.info(f"Bugs enriquecidos: {len(jira_bugs_rich)}")

    # Construir TODO el HTML en Python directamente - sin depender de la IA
    return _build_completion_html(
        project, qa, version, period, fecha,
        jira_bugs_rich, plan_data, context
    )


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
        "secciones numeradas 01-07 (Resumen, Tabla Comparativa con badges badge-ok/badge-partial/badge-fail, "
        "Analisis Detallado, Inconsistencias, Brechas, CPs Propuestos, Conclusiones), report-footer.\n"
        "NO markdown, NO ```html, NO texto plano como instrucciones. Genera HTML real ahora:"
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
        "Estructura: header.header con logo-mark SVG y theme-toggle, div.filter-bar con filter-btns, "
        "main.main con kpi-row 4 KPIs, "
        "section#sec-mejoras con 3-5 cards (card-trigger, card-body con body-grid y body-block), "
        "section#sec-propuestas con 2-4 cards, footer.footer, "
        "script con toggleCard/setFilter/applyFilters/toggleTheme.\n"
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
