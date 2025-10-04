# 🎨 Commit Convention - Emoji Standards

## 📋 Overview
Este proyecto utiliza **emojis estandarizados** en todos los commits para mejorar la legibilidad y comprensión del historial.

---

## ✨ Formato de Commit

```
<emoji> <type>: <description>

[optional body]

[optional footer]
```

### Ejemplo
```
✨ feat: Add Claude Code bidirectional integration

- Implemented HTTP REST API server
- Created claude-aion-cli.js for remote control
- Added 14 API endpoints for full automation

🚀 Generated with Claude Code
Co-Authored-By: Claude <noreply@anthropic.com>
```

---

## 🏷️ Tipos de Commit con Emojis

### ✨ Features (Nuevas Funcionalidades)
```
✨ feat: Add new feature
🎨 style: Improve UI/UX
⚡ perf: Performance improvements
♿ a11y: Accessibility improvements
🌐 i18n: Internationalization
```

### 🐛 Fixes (Correcciones)
```
🐛 fix: Bug fix
🚑 hotfix: Critical hotfix
🔒 security: Security fix
🩹 patch: Simple fix/patch
```

### 📝 Documentation
```
📝 docs: Documentation update
💡 comment: Add/update code comments
📚 docsgen: Generate documentation
```

### 🔧 Configuration & Build
```
🔧 config: Configuration changes
🔨 build: Build system changes
📦 deps: Dependency updates
⬆️ upgrade: Upgrade dependency
⬇️ downgrade: Downgrade dependency
📌 pin: Pin dependencies
```

### 🧪 Testing
```
✅ test: Add/update tests
🧪 experiment: Experimental code
🚨 test-fail: Fix failing tests
🔬 lab: Research/experimental
```

### ♻️ Code Quality
```
♻️ refactor: Refactoring
🎨 format: Code formatting
🧹 cleanup: Code cleanup
🔥 remove: Remove code/files
🚚 move: Move/rename files
```

### 🚀 Deployment & DevOps
```
🚀 deploy: Deployment
🔖 release: Version tag/release
👷 ci: CI/CD changes
💚 ci-fix: Fix CI build
📈 analytics: Add analytics
```

### 🤖 AI & Integration
```
🤖 ai: AI/ML features
🔄 integration: System integration
🔗 api: API changes
📡 network: Network/connectivity
```

### 🎯 Other
```
🔀 merge: Merge branches
⏪ revert: Revert changes
🚧 wip: Work in progress
🏗️ architecture: Architectural changes
🗃️ database: Database changes
💄 ui: UI/styling changes
🔊 logging: Logging improvements
🔇 mute: Remove logging
```

---

## 📏 Reglas

### ✅ DO
- Siempre usa un emoji al inicio del commit
- Usa el tipo correcto (feat, fix, docs, etc.)
- Descripción concisa y clara
- Presente imperativo: "Add feature" not "Added feature"
- Primera letra en minúscula después del tipo
- Máximo 72 caracteres en el título

### ❌ DON'T
- No uses múltiples emojis en el título
- No uses punto final en el título
- No uses mensajes vagos: "Update", "Fix stuff", "WIP"
- No mezcles tipos en un solo commit

---

## 🔍 Ejemplos Correctos

### Features
```bash
✨ feat: implement remote control API server
🤖 ai: add Claude Code CLI integration
⚡ perf: optimize OCR processing with caching
♿ a11y: enhance voice command recognition
```

### Fixes
```bash
🐛 fix: resolve null reference in OCR service
🚑 hotfix: patch critical security vulnerability
🔒 security: sanitize user input in API endpoints
```

### Documentation
```bash
📝 docs: update installation instructions
📚 docsgen: generate API documentation
💡 comment: add XML docs to public methods
```

### DevOps
```bash
🚀 deploy: configure production environment
👷 ci: add automated testing workflow
📦 deps: update .NET to version 8.0.1
🔧 config: add EditorConfig for consistency
```

### Refactoring
```bash
♻️ refactor: extract mouse automation to service
🎨 format: apply consistent code style
🔥 remove: delete deprecated voice commands
```

---

## 🤖 Automatic Validation

### GitHub Actions
```yaml
- name: 🎨 Validate Commit Message
  run: |
    COMMIT_MSG=$(git log -1 --pretty=%B)
    if [[ ! $COMMIT_MSG =~ ^(✨|🐛|📝|🔧|⚡|♻️|🚀|🔥|💚|👷|📦|⬆️|⬇️|🔒|🎨|🤖|🔄) ]]; then
      echo "❌ Commit must start with an emoji!"
      exit 1
    fi
```

### Pre-commit Hook
```bash
#!/bin/bash
COMMIT_MSG_FILE=$1
COMMIT_MSG=$(cat $COMMIT_MSG_FILE)

EMOJI_PATTERN="^(✨|🐛|📝|🔧|⚡|♻️|🚀|🔥|💚|👷|📦|⬆️|⬇️|🔒|🎨|🤖|🔄)"

if ! echo "$COMMIT_MSG" | grep -qE "$EMOJI_PATTERN"; then
  echo "❌ Error: Commit message must start with an emoji!"
  echo "See .github/COMMIT_CONVENTION.md for details"
  exit 1
fi
```

---

## 📊 Statistics

### Most Used Emojis
1. ✨ feat - New features
2. 🐛 fix - Bug fixes
3. 📝 docs - Documentation
4. 🔧 config - Configuration
5. 🤖 ai - AI/Integration

---

## 🔗 Referencias

- [Gitmoji](https://gitmoji.dev/)
- [Conventional Commits](https://www.conventionalcommits.org/)
- [Semantic Versioning](https://semver.org/)
- AION [EMOJI_STYLE_GUIDE.md](../EMOJI_STYLE_GUIDE.md)

---

## ✅ Checklist

Antes de hacer commit:
- [ ] El mensaje empieza con emoji apropiado
- [ ] Incluye tipo (feat, fix, docs, etc.)
- [ ] Descripción clara y concisa
- [ ] Título ≤ 72 caracteres
- [ ] Cuerpo detalla cambios (si aplica)
- [ ] Footer incluye breaking changes (si aplica)

---

<div align="center">

**🎨 Keep commits beautiful, meaningful, and consistent!**

![Emoji Commits](https://img.shields.io/badge/commits-with%20emojis-brightgreen?style=for-the-badge)
![Conventional](https://img.shields.io/badge/conventional-commits-blue?style=for-the-badge)

</div>
