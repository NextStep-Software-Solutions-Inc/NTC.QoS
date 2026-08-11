import os
import re
import subprocess
import sys
import requests
from google import genai

def main():
    gemini_api_key = os.getenv("GEMINI_API_KEY")
    github_token = os.getenv("GH_TOKEN")
    pr_number = os.getenv("PR_NUMBER")
    pr_title = os.getenv("PR_TITLE", "N/A")
    pr_body = os.getenv("PR_BODY", "N/A")
    repo = os.getenv("REPO")
    base_ref = os.getenv("BASE_REF", "main")
    gemini_model = os.getenv("GEMINI_MODEL", "gemini-3.6-flash")

    if not gemini_api_key:
        print("::error::GEMINI_API_KEY secret is missing. Please add GEMINI_API_KEY to repository secrets.")
        sys.exit(1)

    if not github_token:
        print("::error::GH_TOKEN secret is missing. Please add GH_TOKEN to repository secrets.")
        sys.exit(1)

    if not pr_number or not repo:
        print("::error::Required GitHub environment variables are missing.")
        sys.exit(1)

    # Extract Git Diff
    try:
        subprocess.run(["git", "fetch", "origin", base_ref], check=True)
        diff_output = subprocess.check_output(["git", "diff", f"origin/{base_ref}...HEAD"], text=True)
    except Exception as e:
        print(f"::error::Failed to extract git diff: {e}")
        sys.exit(1)

    if not diff_output.strip():
        print("No code changes found in git diff. Skipping review.")
        sys.exit(0)

    # Truncate diff if payload exceeds safety thresholds
    max_lines = 3000
    diff_lines = diff_output.splitlines()
    if len(diff_lines) > max_lines:
        diff_output = "\n".join(diff_lines[:max_lines]) + f"\n\n... [diff truncated, total lines: {len(diff_lines)}]"

    # Call API using Official Google GenAI SDK
    client = genai.Client(api_key=gemini_api_key)

    prompt = f"""You are a senior software engineer performing a code review. Review the following GitHub Pull Request.

  Pull Request Title: {pr_title}
  Pull Request Description: {pr_body}

  Provide a concise, constructive, and actionable review written in a natural human tone:
  ## PR Review Summary
  ### Overview
  ### Code Quality & Architecture
  ### Security & Performance
  ### Recommendations

  IMPORTANT INSTRUCTIONS:
  - Write as a human peer reviewer.
  - DO NOT include emojis like 🤖, robot icons, or mentions of 'AI', 'Gemini', 'automated', 'bot', or AI generation.
  - Keep tone professional, direct, and conversational.

  At the VERY END of your review, on a new line, output your final decision strictly formatted as one of:
  DECISION: APPROVE
  DECISION: REQUEST_CHANGES
  DECISION: COMMENT

  Rules for decision:
  - APPROVE: Code looks clean, safe, well-structured, and ready to merge.
  - REQUEST_CHANGES: Critical security vulnerabilities, breaking changes, or severe bugs detected.
  - COMMENT: Minor feedback or non-blocking suggestions.

  PR Diff:
  ```diff
  {diff_output}
  ```
  """

    print("Sending PR diff for review...")
    try:
        response = client.models.generate_content(
            model=gemini_model,
            contents=prompt,
        )
        review_markdown = response.text
    except Exception as e:
        print(f"::error::API call failed: {e}")
        sys.exit(1)

    if not review_markdown:
        print("::error::Received empty response payload from API.")
        sys.exit(1)

    # Parse decision (APPROVE, REQUEST_CHANGES, or COMMENT)
    event_decision = "COMMENT"
    decision_match = re.search(r"DECISION:\s*(APPROVE|REQUEST_CHANGES|COMMENT)", review_markdown, re.IGNORECASE)
    if decision_match:
        event_decision = decision_match.group(1).upper()
        review_markdown = re.sub(r"DECISION:\s*(APPROVE|REQUEST_CHANGES|COMMENT)", "", review_markdown, flags=re.IGNORECASE).strip()

    print(f"Review Decision: {event_decision}")

    # Submit Official PR Review (APPROVE, REQUEST_CHANGES, or COMMENT) via GitHub REST API
    reviews_url = f"https://api.github.com/repos/{repo}/pulls/{pr_number}/reviews"
    headers = {
        "Authorization": f"Bearer {github_token}",
        "Accept": "application/vnd.github+json",
        "X-GitHub-Api-Version": "2022-11-28",
    }
    payload = {
        "body": review_markdown,
        "event": event_decision
    }

    res = requests.post(reviews_url, headers=headers, json=payload)
    if res.status_code not in (200, 201) and payload["event"] != "COMMENT":
        print(f"::warning::Submitting PR review with event '{payload['event']}' failed ({res.status_code}): {res.text}. Retrying with 'COMMENT' event...")
        payload["event"] = "COMMENT"
        res = requests.post(reviews_url, headers=headers, json=payload)

    if res.status_code in (200, 201):
        print(f"Successfully submitted PR Review ({payload['event']}) to PR #{pr_number}!")
    else:
        print(f"::error::Failed to submit PR review ({res.status_code}): {res.text}")
        sys.exit(1)

if __name__ == "__main__":
    main()
