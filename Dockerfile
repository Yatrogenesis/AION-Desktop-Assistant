# AION Desktop Assistant - Multi-stage Docker Build
# Version: 1.1.0
# Platforms: linux/amd64, linux/arm64

# ============================================================================
# Stage 1: Builder
# ============================================================================
FROM rust:1.75-slim as builder

LABEL maintainer="AION Technologies"
LABEL version="1.1.0"
LABEL description="AI-Powered Desktop Accessibility Assistant"

# Install build dependencies
RUN apt-get update && apt-get install -y \
    pkg-config \
    libx11-dev \
    libxext-dev \
    libxi-dev \
    libxtst-dev \
    && rm -rf /var/lib/apt/lists/*

# Set working directory
WORKDIR /app

# Copy Cargo files for dependency caching
COPY aion-server-rust/Cargo.toml aion-server-rust/Cargo.lock ./

# Create dummy src to build dependencies
RUN mkdir src && \
    echo "fn main() {}" > src/main.rs && \
    cargo build --release && \
    rm -rf src

# Copy actual source code
COPY aion-server-rust/src ./src
COPY aion-server-rust/tests ./tests

# Build the application
RUN cargo build --release && \
    strip target/release/aion-server

# ============================================================================
# Stage 2: Runtime
# ============================================================================
FROM debian:bookworm-slim

LABEL maintainer="AION Technologies"
LABEL version="1.1.0"

# Install runtime dependencies
RUN apt-get update && apt-get install -y \
    libx11-6 \
    libxext6 \
    libxi6 \
    libxtst6 \
    ca-certificates \
    && rm -rf /var/lib/apt/lists/*

# Create non-root user
RUN useradd -m -u 1000 -s /bin/bash aion && \
    mkdir -p /app && \
    chown -R aion:aion /app

# Switch to non-root user
USER aion
WORKDIR /app

# Copy binary from builder
COPY --from=builder --chown=aion:aion /app/target/release/aion-server .

# Expose port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/api/status || exit 1

# Set environment variables
ENV RUST_LOG=info
ENV RUST_BACKTRACE=1

# Run the application
ENTRYPOINT ["./aion-server"]

# Metadata
LABEL org.opencontainers.image.title="AION Desktop Assistant"
LABEL org.opencontainers.image.description="AI-Powered Desktop Accessibility Assistant with HTTP API"
LABEL org.opencontainers.image.version="1.1.0"
LABEL org.opencontainers.image.vendor="AION Technologies"
LABEL org.opencontainers.image.licenses="MIT"
LABEL org.opencontainers.image.source="https://github.com/Yatrogenesis/AION-Desktop-Assistant"
LABEL org.opencontainers.image.documentation="https://github.com/Yatrogenesis/AION-Desktop-Assistant/blob/master/README.md"
