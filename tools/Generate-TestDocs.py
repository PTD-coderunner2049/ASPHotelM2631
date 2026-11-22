#!/usr/bin/env python3
"""
Sinh Test Case doc + Test Result report từ MỘT nguồn: các attribute [TcFact]/[TcTheory] trong tests/**/*.cs
khớp với kết quả chạy (*.trx).  Ai làm tài liệu cũng chạy cùng một lệnh nên format luôn nhất quán.

    python tools/Generate-TestDocs.py [--trx TestResults] [--out docs/testing/generated] [--require-ui-signoff]

Quy tắc "pass ảo là fail" (script trả exit code 1 nếu vi phạm):
  - Test case có trong code nhưng KHÔNG có kết quả (không chạy)                -> NOT RUN  = FAIL
  - Test case UI thiếu ảnh bằng chứng (< 2 ảnh) hoặc chưa có chữ ký quan sát    -> INVALID  = FAIL
  - Kết quả trong TRX có ID không tồn tại trong code (test mồ côi)              -> ORPHAN   = FAIL
  - Test chạy bị Skipped/NotExecuted                                            -> SKIPPED  = FAIL
  - Hai test dùng trùng ID                                                      -> lỗi
"""
import argparse, csv, glob, html, os, re, sys, datetime
import xml.etree.ElementTree as ET

ATTR = re.compile(r'\[\s*Tc(Fact|Theory)\s*\(\s*"(?P<id>[^"]+)"\s*,\s*"(?P<title>(?:[^"\\]|\\.)*)"(?P<rest>.*?)\)\s*\]', re.S)
NAMED = re.compile(r'(\w+)\s*=\s*((?:\$?@?"(?:[^"\\]|\\.)*")|[\w\.]+)')
TC_ID = re.compile(r'^(TC-[A-Z0-9\-]+-\d{3})')
MIN_UI_SHOTS = 2


def unq(v):
    v = v.strip()
    if v.startswith('"') and v.endswith('"'):
        return v[1:-1].replace('\\"', '"').replace('\\\\', '\\')
    return v.split(".")[-1]  # TcMeta.High -> High


def suite_of(path):
    m = re.search(r'ASPHotelM2631\.Tests\.(\w+)', path.replace("\\", "/"))
    return m.group(1) if m else "Other"


def parse_cases(root):
    cases, seen = {}, {}
    for path in sorted(glob.glob(os.path.join(root, "tests", "**", "*.cs"), recursive=True)):
        if "TcAttributes.cs" in path:
            continue
        text = open(path, encoding="utf-8-sig").read()
        for m in ATTR.finditer(text):
            tid = m.group("id")
            if not TC_ID.match(tid):
                sys.exit(f"ID sai quy ước: {tid} ({path}). Dạng đúng: TC-<NHÓM>-<3 chữ số>")
            if tid in seen:
                sys.exit(f"Trùng ID {tid}: {seen[tid]} và {path}")
            seen[tid] = path
            props = {k: unq(v) for k, v in NAMED.findall(m.group("rest"))}
            suite = "Live" if tid.startswith("TC-LIVE-") else suite_of(path)
            cases[tid] = dict(id=tid, title=unq('"' + m.group("title") + '"'), suite=suite,
                              kind="Theory" if m.group(1) == "Theory" else "Fact",
                              req=props.get("Req", ""), priority=props.get("Priority", "Medium"),
                              pre=props.get("Pre", ""), steps=props.get("Steps", ""), expected=props.get("Expected", ""),
                              file=os.path.relpath(path, root).replace("\\", "/"))
    return cases


def parse_trx(trx_dir):
    """Gộp kết quả theo TC-ID. Theory nhiều dòng dữ liệu: fail nếu BẤT KỲ dòng nào fail."""
    res = {}
    files = glob.glob(os.path.join(trx_dir, "**", "*.trx"), recursive=True)
    for f in files:
        for r in ET.parse(f).getroot().iter():
            if not r.tag.endswith("UnitTestResult"):
                continue
            m = TC_ID.match(r.get("testName", ""))
            if not m:
                res.setdefault("__unnamed__", []).append(r.get("testName", ""))
                continue
            tid, outcome = m.group(1), r.get("outcome", "")
            dur = r.get("duration", "00:00:00")
            h, mi, s = dur.split(":")
            secs = int(h) * 3600 + int(mi) * 60 + float(s)
            msg = ""
            for e in r.iter():
                if e.tag.endswith("Message") and e.text:
                    # lay toi da 3 dong dau (xunit: "Values differ" / "Expected: ..." / "Actual: ...")
                    lines = [x.strip() for x in e.text.strip().splitlines() if x.strip()]
                    msg = " | ".join(lines[:3])[:400]
                    break
            d = res.setdefault(tid, dict(total=0, passed=0, failed=0, skipped=0, secs=0.0, msg="", trx=os.path.basename(f)))
            d["total"] += 1; d["secs"] += secs
            if outcome == "Passed": d["passed"] += 1
            elif outcome == "Failed":
                d["failed"] += 1; d["msg"] = d["msg"] or msg
            else: d["skipped"] += 1
    return res, len(files)


def read_csv(path):
    if not os.path.exists(path): return []
    with open(path, encoding="utf-8-sig", newline="") as f:
        return [r for r in csv.DictReader(f) if r and not (list(r.values())[0] or "").startswith("#")]


def evidence_for(tid, ev_dir):
    d = os.path.join(ev_dir, tid)
    if not os.path.isdir(d): return []
    return sorted(x for x in os.listdir(d) if x.lower().endswith(".png"))


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--root", default=".")
    ap.add_argument("--trx", default="TestResults")
    ap.add_argument("--evidence", default=None)
    ap.add_argument("--out", default="docs/testing/generated")
    ap.add_argument("--suites", default="", help="chỉ đánh giá các suite này, vd Unit,Api (CI). Mặc định: tất cả")
    ap.add_argument("--require-ui-signoff", action="store_true", help="bắt buộc có chữ ký xác nhận cho TC UI")
    a = ap.parse_args()
    ev_dir = a.evidence or os.path.join(a.trx, "evidence")
    os.makedirs(a.out, exist_ok=True)

    all_cases = parse_cases(a.root)
    wanted = {x.strip() for x in a.suites.split(",") if x.strip()}
    cases = {k: v for k, v in all_cases.items() if not wanted or v["suite"] in wanted}
    results, n_trx = parse_trx(a.trx)
    unnamed = results.pop("__unnamed__", [])
    defects = {r["tc_id"]: r for r in read_csv(os.path.join(a.root, "docs/testing/defects.csv"))}
    signoff = {r["tc_id"]: r for r in read_csv(os.path.join(a.root, "docs/testing/ui-signoff.csv"))}

    rows = []
    for tid, c in sorted(cases.items()):
        r = results.get(tid)
        status, note = "PASS", ""
        if not r:
            status, note = "NOT RUN", "Không có kết quả trong TRX (không chạy = không tính là pass)"
        elif r["failed"]:
            status, note = "FAIL", f'{r["failed"]}/{r["total"]} lần chạy fail. {r["msg"]}'
        elif r["skipped"]:
            status, note = "SKIPPED", "Test bị bỏ qua - bỏ qua mà vẫn xanh là pass ảo"
        evidence = []
        if c["suite"] == "Ui":
            evidence = evidence_for(tid, ev_dir)
            if status == "PASS":
                if len(evidence) < MIN_UI_SHOTS:
                    status, note = "INVALID", f"Test UI chỉ có {len(evidence)} ảnh bằng chứng (cần >= {MIN_UI_SHOTS})"
                elif a.require_ui_signoff and tid not in signoff:
                    status, note = "INVALID", "Chưa có chữ ký người đã XEM test chạy (docs/testing/ui-signoff.csv)"
        rows.append(dict(c, status=status, note=note, secs=(r or {}).get("secs", 0.0),
                         runs=(r or {}).get("total", 0), evidence=evidence,
                         defect=defects.get(tid, {}).get("defect_id", ""),
                         observer=signoff.get(tid, {}).get("observer", "")))

    orphans = sorted(set(results) - set(all_cases))
    bad = [r for r in rows if r["status"] != "PASS"]
    when = datetime.datetime.now().strftime("%Y-%m-%d %H:%M")
    commit = os.environ.get("GITHUB_SHA", "")[:8] or "local"

    # -------- Test Case doc --------
    md = [f"# Test Case Specification", "",
          f"_Sinh tự động từ code bởi `tools/Generate-TestDocs.py` lúc {when} (commit {commit}). Không sửa tay - sửa attribute `[TcFact]` trong code._", "",
          f"Tổng số test case: **{len(cases)}**" + (f" (suite: {a.suites})" if wanted else ""), ""]
    for suite in sorted({c["suite"] for c in cases.values()}):
        md += [f"## Suite: {suite}", "", "| ID | Tiêu đề | Ưu tiên | Req | Điều kiện | Các bước | Kết quả mong đợi |", "|---|---|---|---|---|---|---|"]
        for c in (c for _, c in sorted(cases.items()) if c["suite"] == suite):
            esc = lambda t: (t or "-").replace("<", "&lt;").replace(">", "&gt;").replace("|", "\\|").replace("\n", " ")
            steps = "<br>".join(esc(x) for x in c["steps"].split(" | ")) if c["steps"] else "-"
            md.append(f'| {c["id"]} | {esc(c["title"])} | {c["priority"]} | {esc(c["req"])} | {esc(c["pre"])} | {steps} | {esc(c["expected"])} |')
        md.append("")
    open(os.path.join(a.out, "TestCases.md"), "w", encoding="utf-8").write("\n".join(md))

    # -------- Test Result (md + html) --------
    counts = {}
    for r in rows: counts[r["status"]] = counts.get(r["status"], 0) + 1
    verdict = "PASSED" if not bad and not orphans and n_trx else "FAILED"
    summ = " | ".join(f"{k}: {v}" for k, v in sorted(counts.items()))
    md = ["# Test Result Report", "", f"- Thời gian: {when}", f"- Commit: {commit}", f"- Số file TRX đọc: {n_trx}",
          f"- Tổng test case: {len(rows)}  ({summ})", f"- **Kết luận: {verdict}**", ""]
    if not n_trx: md += ["> KHÔNG tìm thấy file .trx nào - chưa chạy test hoặc sai đường dẫn --trx. Không có kết quả = FAILED.", ""]
    if orphans: md += [f"> Kết quả của test không có trong tài liệu (ORPHAN): {', '.join(orphans)}", ""]
    if unnamed: md += [f"> Test không dùng [TcFact]/[TcTheory] (không được tính): {len(unnamed)} test", ""]
    md += ["| ID | Suite | Tiêu đề | Kết quả | Số lần chạy | Thời gian (s) | Bug | Ghi chú / lỗi | Bằng chứng |", "|---|---|---|---|---|---|---|---|---|"]
    for r in rows:
        ev = f'{len(r["evidence"])} ảnh' + (f' · xác nhận: {r["observer"]}' if r["observer"] else "") if r["suite"] == "Ui" else "-"
        md.append(f'| {r["id"]} | {r["suite"]} | {r["title"].replace("|","/")} | {r["status"]} | {r["runs"]} | {r["secs"]:.2f} | {r["defect"] or "-"} | {r["note"].replace("|","/") or "-"} | {ev} |')
    open(os.path.join(a.out, "TestResult.md"), "w", encoding="utf-8").write("\n".join(md))

    color = {"PASS": "#1a7f37", "FAIL": "#cf222e", "NOT RUN": "#9a6700", "SKIPPED": "#9a6700", "INVALID": "#8250df"}
    h = [f"<!doctype html><meta charset=utf-8><title>Test Result</title><style>body{{font:14px system-ui;margin:24px}}table{{border-collapse:collapse;width:100%}}td,th{{border:1px solid #d0d7de;padding:6px 8px;vertical-align:top}}th{{background:#f6f8fa}}.b{{color:#fff;padding:2px 8px;border-radius:10px;font-weight:600}}img{{height:90px;margin:2px;border:1px solid #ccc}}</style>",
         f"<h1>Test Result Report</h1><p>{when} · commit {commit} · {html.escape(summ)}</p><h2 style='color:{'#1a7f37' if verdict=='PASSED' else '#cf222e'}'>Kết luận: {verdict}</h2>"]
    h.append("<table><tr><th>ID<th>Suite<th>Tiêu đề<th>Kết quả<th>Lần chạy<th>Giây<th>Bug<th>Ghi chú<th>Bằng chứng</tr>")
    for r in rows:
        imgs = "".join(f'<a href="../../../{a.trx}/evidence/{r["id"]}/{e}"><img src="../../../{a.trx}/evidence/{r["id"]}/{e}"></a>' for e in r["evidence"])
        h.append(f'<tr><td>{r["id"]}<td>{r["suite"]}<td>{html.escape(r["title"])}<td><span class=b style="background:{color.get(r["status"],"#444")}">{r["status"]}</span>'
                 f'<td>{r["runs"]}<td>{r["secs"]:.2f}<td>{r["defect"] or "-"}<td>{html.escape(r["note"]) or "-"}<td>{imgs or "-"}</tr>')
    h.append("</table>")
    open(os.path.join(a.out, "TestResult.html"), "w", encoding="utf-8").write("".join(h))

    print(f"[docs] {len(cases)} test case, {n_trx} TRX, kết luận {verdict} ({summ})")
    for r in bad: print(f'  - {r["id"]}: {r["status"]} {r["note"]}')
    for o in orphans: print(f"  - {o}: ORPHAN")
    sys.exit(0 if verdict == "PASSED" else 1)


if __name__ == "__main__":
    main()
