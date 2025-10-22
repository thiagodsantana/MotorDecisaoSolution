terraform {
  required_version = ">= 1.8.0"

  required_providers {
    google = {
      source  = "hashicorp/google"
      version = ">= 7.8.0"
    }
  }
}

provider "google" {
  project = var.project_id
  region  = var.region
}

variable "project_id" {
  default = "motordecisao"
}

variable "region" {
  default = "us-central1"
}

##############################
# SERVICE ACCOUNTS
##############################

resource "google_service_account" "run_sa" {
  account_id   = "motor-decisao-run-sa"
  display_name = "Service Account Cloud Run API"
}

resource "google_service_account" "function_sa" {
  account_id   = "motor-decisao-function-sa"
  display_name = "Service Account Cloud Function Gen2"
}

##############################
# BUCKETS PÚBLICOS
##############################

resource "google_storage_bucket" "input" {
  name                        = "motor-decisao-input"
  location                    = var.region
  force_destroy               = true
  uniform_bucket_level_access = true
}

resource "google_storage_bucket_iam_member" "input_public" {
  bucket = google_storage_bucket.input.name
  role   = "roles/storage.objectViewer"
  member = "allUsers"
}

resource "google_storage_bucket" "output" {
  name                        = "motor-decisao-output"
  location                    = var.region
  force_destroy               = true
  uniform_bucket_level_access = true
}

resource "google_storage_bucket_iam_member" "output_public" {
  bucket = google_storage_bucket.output.name
  role   = "roles/storage.objectViewer"
  member = "allUsers"
}

# Cloud Run e Cloud Function permissões
resource "google_storage_bucket_iam_member" "run_read_input" {
  bucket = google_storage_bucket.input.name
  role   = "roles/storage.objectViewer"
  member = "serviceAccount:${google_service_account.run_sa.email}"
}

resource "google_storage_bucket_iam_member" "run_write_output" {
  bucket = google_storage_bucket.output.name
  role   = "roles/storage.objectAdmin"
  member = "serviceAccount:${google_service_account.run_sa.email}"
}

resource "google_storage_bucket_iam_member" "function_write_output" {
  bucket = google_storage_bucket.output.name
  role   = "roles/storage.objectAdmin"
  member = "serviceAccount:${google_service_account.function_sa.email}"
}

##############################
# CLOUD RUN – API
##############################

resource "google_cloud_run_v2_service" "api" {
  name                = "motor-decisao-api"
  location            = var.region
  deletion_protection = false

  template {
    containers {
      image = "gcr.io/${var.project_id}/motor-decisao-api:1.0"

      env {
        name  = "BucketInput"
        value = google_storage_bucket.input.name
      }

      env {
        name  = "BucketOutput"
        value = google_storage_bucket.output.name
      }
    }

    service_account = google_service_account.run_sa.email
  }

  ingress = "INGRESS_TRAFFIC_ALL"
}

resource "google_cloud_run_v2_service_iam_member" "public_invoker" {
  name     = google_cloud_run_v2_service.api.name
  location = var.region
  role     = "roles/run.invoker"
  member   = "allUsers"
}

##############################
# CLOUD FUNCTION GEN2
##############################

resource "google_cloudfunctions2_function" "processa_proposta" {
  name     = "funcaoProcessaProposta"
  location = var.region

  build_config {
    runtime     = "dotnet8"
    entry_point = "FunctionEntryPoint"

    docker_registry = "CONTAINER_REGISTRY"
    image_uri       = "gcr.io/${var.project_id}/motor-decisao-function:1.0"
  }

  service_config {
    available_memory      = "256M"
    service_account_email = google_service_account.function_sa.email
    environment_variables = {
      BUCKET_OUTPUT = google_storage_bucket.output.name
    }
  }

  event_trigger {
    event_type = "google.cloud.storage.object.v1.finalized"

    event_filters {
      attribute = "bucket"
      value     = google_storage_bucket.input.name
    }

    retry_policy = "RETRY_POLICY_RETRY"
  }
}

##############################
# IAM ADMIN
##############################

resource "google_project_iam_member" "run_sa_admin" {
  project = var.project_id
  role    = "roles/run.admin"
  member  = "serviceAccount:${google_service_account.run_sa.email}"
}

resource "google_project_iam_member" "function_sa_admin" {
  project = var.project_id
  role    = "roles/cloudfunctions.admin"
  member  = "serviceAccount:${google_service_account.function_sa.email}"
}

##############################
# OUTPUTS
##############################

output "cloud_run_url" {
  value       = google_cloud_run_v2_service.api.uri
  description = "URL pública da API Cloud Run"
}

output "cloud_function_name" {
  value       = google_cloudfunctions2_function.processa_proposta.name
  description = "Nome da Cloud Function Gen2"
}

output "bucket_input_url" {
  value       = "https://storage.googleapis.com/${google_storage_bucket.input.name}/"
}

output "bucket_output_url" {
  value       = "https://storage.googleapis.com/${google_storage_bucket.output.name}/"
}
