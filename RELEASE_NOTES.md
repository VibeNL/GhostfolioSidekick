# Release Notes

## 2026-11-22 — Chiseled image (non-root, unified host)

### ⚠️ Breaking change for upgraders: container now runs as non-root (uid `1654`)

If you are upgrading from an older image version, your data volume is probably owned by `root` and the new container cannot write to it. Before starting the new image, change ownership once:

```bash
docker run --rm -v /volume1/docker/ghostfolio/sidekick:/data alpine chown 1654:1654 /data
```

(Replace `/volume1/docker/ghostfolio/sidekick` with your host path.) If you skip this, the container will start but the importer cannot open its database until ownership is fixed; the logs contain an error message telling you exactly what to run.

### What changed

The image is now built on a .NET 11 chiseled base and runs the sidekick importer **and** the PortfolioViewer web app in one process:

- **Non-root user (uid `1654`)** — old root-owned volumes need a one-time `chown` (see above).
- **PortfolioViewer is now served from this container** on port 80 (http) and 443 (https, self-signed certificate baked into the image). Open `http://<your-host>` to view your portfolio. Browsers will warn about the https self-signed cert — use http or put your own reverse-proxy TLS in front if you want a trusted connection.
- **Smaller image and lower memory usage** thanks to the chiseled (distroless) base.
- **No shell inside the container** (`docker exec <container> sh` no longer works). Debug via `docker logs`; misconfiguration produces clear error messages (missing `GHOSTFOLIO_URL`, unwritable volume).

If you don't need the PortfolioViewer web app, simply don't publish ports 80/443 in your compose file — the importer keeps working without them.
