# CoreWeb

Reusable web projects (C# Razor class libraries + TypeScript) extracted from [MrWatchdog](https://github.com/xhafan/MrWatchdog).

## Projects

- `CoreWeb` — Razor class library with shared views, tag helpers, Stimulus controllers, and TypeScript utilities
- `CoreWeb.Account` — login-link account flow on top of `CoreWeb`

## Dependency on CoreBackend

Both projects depend on `CoreBackend` (projects `CoreBackend` and `CoreBackend.Account`). This repo does not build standalone — consumers must also have the [CoreBackend](https://github.com/xhafan/CoreBackend) subtree vendored at `src/Libraries/CoreBackend/`.

## Consumption

```bash
git subtree add  --prefix=src/Libraries/CoreWeb https://github.com/xhafan/CoreWeb main --squash
git subtree pull --prefix=src/Libraries/CoreWeb https://github.com/xhafan/CoreWeb main --squash
git subtree push --prefix=src/Libraries/CoreWeb https://github.com/xhafan/CoreWeb main
```

## TypeScript

Each project has its own `tsconfig.json` and `package.json`. Consumers bundle TypeScript (e.g. via esbuild in their web app entry project); the Razor class libraries do not ship pre-bundled JavaScript.

Required npm devDependencies (installed in the consumer's web app):

- `typescript`
- `@hotwired/stimulus`
- `@types/jquery`, `@types/bootbox`
- `linq`
- `stacktrace-js`
