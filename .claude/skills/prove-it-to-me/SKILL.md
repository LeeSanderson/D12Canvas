---
name: prove-it-to-me
description: Triggered when the user is skeptical or suspicious of a previous response. The
LLM must step back, self-audit its last message, and provide empirical evidence (code
execution, file reads, or search) to verify its claims. If a claim is found to be incorrect
during this audit, the LLM must own the error and provide a correction.
disable-model-invocation: true
---

# Prove It To Me

The user has a "sneaky feeling" or suspicion about the veracity of your previous response.
You are now in an evidentiary mindset. Your goal is to move beyond prose and provide
"receipts" for your claims.

### Operational Guidelines

* **Self-Audit:** Review your previous response in its entirety. Identify every specific
  "truth claim" you made—such as the existence of a file, the syntax of a library, the result
  of a calculation, or a factual event.
* **Prioritize Empirical Proof:** * If it's **code**, run it (or a test case of it) to
  verify the output.
    * If it's a **file or codebase**, use tools to `cat`, `grep`, or `ls` the actual
      content.
    * If it's an **external fact**, perform a fresh search and provide the specific source
      or snippet.
* **Maintain Space:** You are not restricted to a specific table or format. Choose the most
  convincing way to present your proof based on the situation. Use raw tool outputs (terminal
  logs, raw text) as "receipts" so the user doesn't have to take your word for it.
* **Handling Discrepancies:** If your audit reveals you were wrong, do not hide it.
  Explicitly state what was incorrect, show the evidence that disproved it, and provide the
  corrected information.
* **Clarification & Delegation:** If you are unsure what exactly the user is suspicious of,
  ask a clarifying question. If a proof requires user-side access (e.g., local network state),
  provide the exact command for the user to run so they can verify it themselves.

**Objective:** Transform the interaction from a narrative to a verifiable forensic report.