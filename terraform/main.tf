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

# Ativa a API do Artifact Registry
resource "google_project_service" "artifact_registry" {
  service            = "artifactregistry.googleapis.com"
  disable_on_destroy = false
}

# Ativa a API do Cloud Run
resource "google_project_service" "cloud_run" {
  service            = "run.googleapis.com"
  disable_on_destroy = false
}

# Cria o Repositório no Artifact Registry
resource "google_artifact_registry_repository" "sarabank_repo" {
  depends_on    = [google_project_service.artifact_registry]
  location      = "us-central1"
  repository_id = "sarabank-repo"
  description   = "Repositorio Docker para o SaraBank"
  format        = "DOCKER"
}

# =================================================================
# 2. CONFIGURAÇÃO DO BANCO DE DADOS FIRESTORE (MODO NATIVO)
# =================================================================
resource "google_firestore_database" "database" {
  project     = var.project_id
  name        = "(default)" # O ID padrão obrigatório
  location_id = var.region
  type        = "FIRESTORE_NATIVE"

  depends_on = [google_project_service.firestore]
}

# =================================================================
# 3. ÍNDICES COMPOSTOS (ESSENCIAIS PARA O CÓDIGO .NET)
# =================================================================

# --- COLEÇÃO: Movimentacoes ---

# Consulta: .WhereEqualTo("ContaId", id).OrderByDescending("Data")
resource "google_firestore_index" "movimentacoes_extrato_index" {
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

# Consulta: .WhereEqualTo("SagaId", id).WhereEqualTo("Tipo", tipo)
resource "google_firestore_index" "movimentacoes_saga_idempotencia_index" {
  project    = var.project_id
  collection = "Movimentacoes"

  fields {
    field_path = "SagaId"
    order      = "ASCENDING"
  }

  fields {
    field_path = "Tipo"
    order      = "ASCENDING"
  }
  depends_on = [google_firestore_database.database]
}

# --- COLEÇÃO: Outbox ---

# Campos: Processado (ASC), Tentativas (ASC), __name__ (ASC)
resource "google_firestore_index" "outbox_worker_index" {
  project    = var.project_id
  collection = "Outbox"

  fields {
    field_path = "Processado"
    order      = "ASCENDING"
  }

  fields {
    field_path = "Tentativas"
    order      = "ASCENDING"
  }

  # Removido CriadoEm para bater com a query exata do log de erro
  depends_on = [google_firestore_database.database]
}

# =================================================================
# 4. PUB/SUB - TÓPICOS 
# =================================================================

# Mantemos os tópicos existentes...
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

# --- TÓPICOS DA SAGA ---

# 1. Início da Saga (API -> Worker de Débito)
resource "google_pubsub_topic" "transferencias_iniciadas_topic" {
  name    = "sara-bank-transferencias-iniciadas"
  project = var.project_id
  labels  = { app = "sarabank", saga_step = "started" }
  depends_on = [google_project_service.pubsub]
}

# 2. Próximo Passo (Worker de Débito -> Worker de Crédito)
resource "google_pubsub_topic" "transferencias_debitadas_topic" {
  name    = "sara-bank-transferencias-debitadas"
  project = var.project_id
  labels  = { app = "sarabank", saga_step = "debited" }
  depends_on = [google_project_service.pubsub]
}

# 3. Caminho de Compensação (Worker de Crédito -> Worker de Estorno)
resource "google_pubsub_topic" "transferencias_compensar_topic" {
  name    = "sara-bank-transferencias-compensar"
  project = var.project_id
  labels  = { app = "sarabank", saga_step = "compensate" }
  depends_on = [google_project_service.pubsub]
}

# 4. Falha Crítica de Negócio (ex: Saldo Insuficiente na Origem)
resource "google_pubsub_topic" "transferencias_erros_topic" {
  name    = "sara-bank-transferencias-erros"
  project = var.project_id
  labels  = { app = "sarabank", saga_step = "failed" }
  depends_on = [google_project_service.pubsub]
}

# 5. Finalização com Sucesso (Auditoria/Notificação)
resource "google_pubsub_topic" "transferencias_concluidas_topic" {
  name    = "sara-bank-transferencias-concluidas"
  project = var.project_id
  labels  = { app = "sarabank", saga_step = "finished" }
  depends_on = [google_project_service.pubsub]
}

# =================================================================
# 5. PUB/SUB - CONSUMIDORES
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

# --- SAGA ---

# Consumer que realizará o DÉBITO
resource "google_pubsub_subscription" "transferencias_iniciadas_sub" {
  name                 = "sara-bank-transferencias-iniciadas-sub"
  topic                = google_pubsub_topic.transferencias_iniciadas_topic.name
  project              = var.project_id
  ack_deadline_seconds = 30 
}

# Consumer que realizará o CRÉDITO
resource "google_pubsub_subscription" "transferencias_debitadas_sub" {
  name                 = "sara-bank-transferencias-debitadas-sub"
  topic                = google_pubsub_topic.transferencias_debitadas_topic.name
  project              = var.project_id
  ack_deadline_seconds = 30
}

# Consumer que realizará o ESTORNO (Compensação)
resource "google_pubsub_subscription" "transferencias_compensar_sub" {
  name                 = "sara-bank-transferencias-compensar-sub"
  topic                = google_pubsub_topic.transferencias_compensar_topic.name
  project              = var.project_id
  ack_deadline_seconds = 30
}

# Subscription para monitorar cancelamentos (pode disparar Push Notification de erro)
resource "google_pubsub_subscription" "transferencias_erros_sub" {
  name                 = "sara-bank-transferencias-erros-sub"
  topic                = google_pubsub_topic.transferencias_erros_topic.name
  project              = var.project_id
  ack_deadline_seconds = 20
}

# Subscription para monitorar conclusões (pode disparar Comprovante por e-mail)
resource "google_pubsub_subscription" "transferencias_concluidas_sub" {
  name                 = "sara-bank-transferencias-concluidas-sub"
  topic                = google_pubsub_topic.transferencias_concluidas_topic.name
  project              = var.project_id
  ack_deadline_seconds = 30
}