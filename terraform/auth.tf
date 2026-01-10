# 1. Habilita a API necessária para o serviço de identidade
resource "google_project_service" "identity_toolkit" {
  service            = "identitytoolkit.googleapis.com"
  disable_on_destroy = false
}

# 2. Configura o Identity Platform (Nosso "Identity Server")
resource "google_identity_platform_config" "auth_config" {
  project = var.project_id

  signin {
    allow_duplicate_emails = false

    email {
      enabled           = true
      password_required = true
    }
  }

  # domínios autorizados para evitar acessos externos maliciosos
  authorized_domains = [
    "localhost",
    "127.0.0.1",
    "${var.project_id}.firebaseapp.com",
    "${var.project_id}.web.app"
  ]

  depends_on = [google_project_service.identity_toolkit]
}