# =================================================================
# 1. ATIVAÇÃO DAS APIS DO GOOGLE CLOUD
# =================================================================
resource "google_project_service" "firestore" {
  service            = "firestore.googleapis.com"
  disable_on_destroy = false
}

resource "google_project_service" "pubsub" {
  service            = "pubsub.googleapis.com"
  disable_on_destroy = false
}

# =================================================================
# 2. CONFIGURAÇÃO DO BANCO DE DADOS FIRESTORE (MODO NATIVO)
# =================================================================
resource "google_firestore_database" "database" {
  project     = var.project_id
  name        = "(default)" # O ID padrão obrigatório
  location_id = var.region
  type        = "FIRESTORE_NATIVE"

  # O erro 400 sugere aguardar a propagação da exclusão/ativação
  depends_on = [google_project_service.firestore]
}

# =================================================================
# 3. ÍNDICES COMPOSTOS (ESSENCIAIS PARA O CÓDIGO .NET)
# =================================================================

# Índice para Extrato: .WhereEqualTo("ContaId", id).OrderByDescending("Data")
resource "google_firestore_index" "movimentacoes_index" {
  project    = var.project_id
  collection = "Movimentacoes"

  fields {
    field_path = "ContaId"
    order      = "ASCENDING"
  }

  fields {
    field_path = "Data"
    order      = "DESCENDING"
  }
  depends_on = [google_firestore_database.database]
}

# Índice para o Worker do Outbox: .WhereEqualTo("Processado", false).OrderBy("CriadoEm")
resource "google_firestore_index" "outbox_index" {
  project    = var.project_id
  collection = "Outbox"

  fields {
    field_path = "Processado"
    order      = "ASCENDING"
  }

  fields {
    field_path = "CriadoEm"
    order      = "ASCENDING"
  }
  depends_on = [google_firestore_database.database]
}

# =================================================================
# 4. PUB/SUB - TÓPICOS (AS ESTEIRAS DE EVENTOS)
# =================================================================

resource "google_pubsub_topic" "usuarios_topic" {
  name    = "sara-bank-usuarios"
  project = var.project_id
  labels  = { app = "sarabank", context = "users" }
  depends_on = [google_project_service.pubsub]
}

resource "google_pubsub_topic" "movimentacoes_topic" {
  name    = "sara-bank-movimentacoes"
  project = var.project_id
  labels  = { app = "sarabank", context = "finance" }
  depends_on = [google_project_service.pubsub]
}

resource "google_pubsub_topic" "transferencias_topic" {
  name    = "sara-bank-transferencias"
  project = var.project_id
  labels  = { app = "sarabank", context = "transfers" }
  depends_on = [google_project_service.pubsub]
}

# =================================================================
# 5. PUB/SUB - ASSINATURAS (OS CONSUMIDORES)
# =================================================================

resource "google_pubsub_subscription" "usuarios_sub" {
  name                 = "sara-bank-usuarios-sub"
  topic                = google_pubsub_topic.usuarios_topic.name
  project              = var.project_id
  ack_deadline_seconds = 20
}

resource "google_pubsub_subscription" "movimentacoes_sub" {
  name                 = "sara-bank-movimentacoes-sub"
  topic                = google_pubsub_topic.movimentacoes_topic.name
  project              = var.project_id
  ack_deadline_seconds = 20
}

resource "google_pubsub_subscription" "transferencias_sub" {
  name                 = "sara-bank-transferencias-sub"
  topic                = google_pubsub_topic.transferencias_topic.name
  project              = var.project_id
  ack_deadline_seconds = 30 # Mais tempo para processar as duas pernas da transferência
}