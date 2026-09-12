"""Validate the source fork without changing the frozen upstream-baseline tooling."""
import baseline as b


def check_upstream(expected_sha: str) -> None:
    lock = b.read_json(b.PORT / "source-lock.json")
    if lock["upstreamCommit"] != expected_sha:
        raise RuntimeError("Source port changed its locked upstream baseline")
    expected = lock["sourceCommit"]
    actual = b.run(["git", "-C", b.UPSTREAM, "rev-parse", "HEAD"])
    if actual != expected:
        raise RuntimeError(f"Source revision mismatch: expected {expected}, got {actual}")
    b.run(["git", "-C", b.UPSTREAM, "merge-base", "--is-ancestor", expected_sha, expected])
    status = b.run(["git", "-C", b.UPSTREAM, "status", "--porcelain=v1", "--untracked-files=all"])
    if status:
        raise RuntimeError("Commit reviewed source changes and update source-lock.json before acceptance: " + status[:1500])
    gitlink = b.run(["git", "-C", b.ROOT, "ls-files", "--stage", "External/SurvivalcraftApi"])
    if not gitlink.startswith(f"160000 {expected} "):
        raise RuntimeError("Parent gitlink differs from the locked source commit")
