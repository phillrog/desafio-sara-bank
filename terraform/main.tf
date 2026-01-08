# 1. Ativação das APIs do Google Cloud
resource "google_project_service" "firestore" {
  service            = "firestore.googleapis.com"
  disable_on_destroy = false
}

resource "google_project_service" "pubsub" {
  service            = "pubsub.googleapis.com"
  disable_on_destroy = false
}

# 2. Configuração do Banco de Dados Firestore (Modo Nativo)
resource "google_firestore_database" "database" {
  project     = var.project_id
  name        = "(default)"
  location_id = var.region
  type        = "FIRESTORE_NATIVE"

  # Garante que as APIs foram ativadas antes de criar o banco
  depends_on = [google_project_service.firestore]
}

# 3. Índice Composto para o Extrato de Movimentações
# Necessário para: .WhereEqualTo("ContaId", id).OrderByDescending("Data")
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
}

# 4. Índice para o Outbox Worker
# Necessário para: .WhereEqualTo("Processado", false).OrderBy("CriadoEm")
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
}

# 5. Pub/Sub - Tópico de Eventos Financeiros
resource "google_pubsub_topic" "transacoes_topic" {
  name    = "sara-bank-transacoes-topic"
  project = var.project_id

  labels = {
    environment = "dev"
    app         = "sarabank"
  }

  depends_on = [google_project_service.pubsub]
}

# 6. Pub/Sub - Assinatura para Consumidores (ex: Notificações)
resource "google_pubsub_subscription" "notificacoes_sub" {
  name    = "sara-bank-notificacoes-sub"
  topic   = google_pubsub_topic.transacoes_topic.name
  project = var.project_id

  # Tempo para o subscriber confirmar o processamento
  ack_deadline_seconds = 20

  # Retenção de mensagens por 7 dias se não houver confirmação
  message_retention_duration = "604800s"
}