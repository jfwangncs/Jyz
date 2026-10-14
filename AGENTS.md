# AGENTS.md

This file provides guidance to AI coding agents when working with this repository. It follows the [agents.md](https://agents.md) standard so any compliant tool can read it directly.

## Project Overview

This is a full-stack application generated from the JfYu.Template.

- **`src/api/`** — ASP.NET Core 10 Web API backend. See `src/api/AGENTS.md` for stack-specific conventions.
- **`src/web/`** — Vue 3 + TypeScript + Vite monorepo frontend. See `src/web/AGENTS.md` for stack-specific conventions.

## Working Across the Stack


When a feature spans both frontend and backend:

1. Define the API contract (request/response models) in the `src/api` project first.
2. Implement the backend endpoint with proper validation, service logic, and response wrappers.
3. Add the corresponding API call and UI in the `src/web` project.

For a complete, tool-agnostic step-by-step workflow, see [`docs/fullstack-feature-guide.md`](docs/fullstack-feature-guide.md).

### How to use the feature guide

This guide does not auto-trigger — tell your AI coding agent to follow it explicitly when starting a new feature module, e.g.:

> Read `docs/fullstack-feature-guide.md` and follow it step by step to implement a `<FeatureName>` module.

The guide contains checkpoints where the agent should pause and ask you to review DB fields and permission codes before continuing — expect to be prompted mid-way, not just at the end.
