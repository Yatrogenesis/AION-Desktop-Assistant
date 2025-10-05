# AION Desktop Assistant - Makefile
# Multi-platform build automation
# Version: 1.1.0

.PHONY: help build test clean install release docker all

# Configuration
VERSION := 1.1.0
RUST_DIR := aion-server-rust
CARGO := cargo
PYTHON := python

# Platform detection
ifeq ($(OS),Windows_NT)
    PLATFORM := windows
    BINARY_EXT := .exe
    RM := cmd /c del /Q /F
    RMDIR := cmd /c rmdir /S /Q
else
    UNAME_S := $(shell uname -s)
    ifeq ($(UNAME_S),Linux)
        PLATFORM := linux
    endif
    ifeq ($(UNAME_S),Darwin)
        PLATFORM := macos
    endif
    BINARY_EXT :=
    RM := rm -f
    RMDIR := rm -rf
endif

# Colors for output
RED := \033[0;31m
GREEN := \033[0;32m
YELLOW := \033[0;33m
BLUE := \033[0;34m
NC := \033[0m # No Color

help: ## Show this help message
	@echo "$(BLUE)╔════════════════════════════════════════════════════╗$(NC)"
	@echo "$(BLUE)║  AION Desktop Assistant - Build System v$(VERSION)  ║$(NC)"
	@echo "$(BLUE)╚════════════════════════════════════════════════════╝$(NC)"
	@echo ""
	@echo "$(GREEN)Available targets:$(NC)"
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) | awk 'BEGIN {FS = ":.*?## "}; {printf "  $(YELLOW)%-20s$(NC) %s\n", $$1, $$2}'
	@echo ""
	@echo "$(GREEN)Platform:$(NC) $(PLATFORM)"
	@echo "$(GREEN)Version:$(NC) $(VERSION)"

all: clean build test ## Clean, build, and test everything

build: ## Build Rust server (release mode)
	@echo "$(BLUE)🔨 Building Rust server...$(NC)"
	cd $(RUST_DIR) && $(CARGO) build --release
	@echo "$(GREEN)✅ Build complete!$(NC)"

build-debug: ## Build Rust server (debug mode)
	@echo "$(BLUE)🔨 Building Rust server (debug)...$(NC)"
	cd $(RUST_DIR) && $(CARGO) build
	@echo "$(GREEN)✅ Debug build complete!$(NC)"

test: ## Run all tests
	@echo "$(BLUE)🧪 Running Rust tests...$(NC)"
	cd $(RUST_DIR) && $(CARGO) test --verbose
	@echo ""
	@echo "$(BLUE)🐍 Running Python tests...$(NC)"
	$(PYTHON) -m pytest tests/ -v
	@echo "$(GREEN)✅ All tests passed!$(NC)"

test-rust: ## Run Rust tests only
	@echo "$(BLUE)🧪 Running Rust tests...$(NC)"
	cd $(RUST_DIR) && $(CARGO) test --verbose

test-python: ## Run Python tests only
	@echo "$(BLUE)🐍 Running Python tests...$(NC)"
	$(PYTHON) -m pytest tests/ -v

test-coverage: ## Run tests with coverage
	@echo "$(BLUE)📊 Running tests with coverage...$(NC)"
	cd $(RUST_DIR) && $(CARGO) tarpaulin --out Html
	$(PYTHON) -m pytest tests/ --cov=. --cov-report=html

lint: ## Run linters
	@echo "$(BLUE)🔍 Running Rust linter...$(NC)"
	cd $(RUST_DIR) && $(CARGO) clippy -- -D warnings
	@echo "$(BLUE)🔍 Running Python linter...$(NC)"
	$(PYTHON) -m black --check .
	$(PYTHON) -m flake8 .

format: ## Format code
	@echo "$(BLUE)🎨 Formatting Rust code...$(NC)"
	cd $(RUST_DIR) && $(CARGO) fmt
	@echo "$(BLUE)🎨 Formatting Python code...$(NC)"
	$(PYTHON) -m black .

clean: ## Clean build artifacts
	@echo "$(BLUE)🧹 Cleaning build artifacts...$(NC)"
	cd $(RUST_DIR) && $(CARGO) clean
	-$(RMDIR) tests/__pycache__ 2>nul || true
	-$(RMDIR) tests/.pytest_cache 2>nul || true
	-$(RM) tests/*.pyc 2>nul || true
	@echo "$(GREEN)✅ Clean complete!$(NC)"

install: build ## Install to system
ifeq ($(PLATFORM),windows)
	@echo "$(YELLOW)⚠️  Windows installation requires Inno Setup$(NC)"
	@echo "$(BLUE)Use: iscc installers/installer-win-x64.iss$(NC)"
else ifeq ($(PLATFORM),linux)
	@echo "$(BLUE)📦 Installing to Linux system...$(NC)"
	chmod +x installers/install-linux-x64.sh
	sudo installers/install-linux-x64.sh
else ifeq ($(PLATFORM),macos)
	@echo "$(BLUE)📦 Installing to macOS system...$(NC)"
	chmod +x installers/install-macos-universal.sh
	sudo installers/install-macos-universal.sh
endif

run: build ## Build and run the server
	@echo "$(BLUE)🚀 Starting AION server...$(NC)"
	cd $(RUST_DIR) && $(CARGO) run --release

dev: ## Run in development mode with auto-reload
	@echo "$(BLUE)🔥 Starting development server...$(NC)"
	cd $(RUST_DIR) && $(CARGO) watch -x run

release-prep: clean test ## Prepare for release (clean, test)
	@echo "$(GREEN)✅ Ready for release!$(NC)"
	@echo "$(BLUE)Next steps:$(NC)"
	@echo "  1. Update version.json"
	@echo "  2. Create git tag: git tag v$(VERSION)"
	@echo "  3. Push tag: git push origin v$(VERSION)"
	@echo "  4. GitHub Actions will create the release"

docker-build: ## Build Docker image
	@echo "$(BLUE)🐳 Building Docker image...$(NC)"
	docker build -t aion-desktop-assistant:$(VERSION) .
	docker tag aion-desktop-assistant:$(VERSION) aion-desktop-assistant:latest
	@echo "$(GREEN)✅ Docker image built!$(NC)"

docker-run: ## Run Docker container
	@echo "$(BLUE)🐳 Starting Docker container...$(NC)"
	docker run -p 8080:8080 --name aion-server aion-desktop-assistant:latest

benchmark: ## Run performance benchmarks
	@echo "$(BLUE)⚡ Running benchmarks...$(NC)"
	cd $(RUST_DIR) && $(CARGO) bench

audit: ## Security audit
	@echo "$(BLUE)🔒 Running security audit...$(NC)"
	cd $(RUST_DIR) && $(CARGO) audit

update-deps: ## Update dependencies
	@echo "$(BLUE)📦 Updating Rust dependencies...$(NC)"
	cd $(RUST_DIR) && $(CARGO) update
	@echo "$(BLUE)📦 Updating Python dependencies...$(NC)"
	$(PYTHON) -m pip install --upgrade -r tests/requirements-test.txt

size: build ## Show binary size
	@echo "$(BLUE)📊 Binary size:$(NC)"
ifeq ($(PLATFORM),windows)
	@dir $(RUST_DIR)\target\release\aion-server.exe | findstr "aion-server.exe"
else
	@ls -lh $(RUST_DIR)/target/release/aion-server | awk '{print $$5, $$9}'
endif

check: ## Check code without building
	@echo "$(BLUE)🔍 Checking code...$(NC)"
	cd $(RUST_DIR) && $(CARGO) check --all-targets

ci: clean lint test ## Run CI pipeline locally
	@echo "$(GREEN)✅ CI pipeline complete!$(NC)"

.DEFAULT_GOAL := help
