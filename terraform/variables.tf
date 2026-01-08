variable "project_id" {
  description = "O ID do seu projeto no Google Cloud"
  type        = string
}

variable "region" {
  description = "A região onde os recursos serão criados"
  type        = string
  default     = "southamerica-east1" # São Paulo
}