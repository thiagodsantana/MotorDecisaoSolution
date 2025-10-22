# Cloud Run API (public)
resource "google_cloud_run_v2_service" "api" {
  name                = "motor-decisao-api"
  location            = var.region
  deletion_protection = false

  template {
    containers {
      image = "gcr.io/${var.project_id}/motor-decisao-api:1.0"

      env {
        name  = "BUCKET_INPUT"
        value = google_storage_bucket.input.name
      }

      env {
        name  = "BUCKET_OUTPUT"
        value = google_storage_bucket.output.name
      }
    }

    service_account = google_service_account.run_sa.email
  }

  ingress = "INGRESS_TRAFFIC_ALL"

  lifecycle {
    ignore_changes = [template[0].containers[0].image]
  }
}

resource "google_cloud_run_v2_service_iam_member" "api_public_invoker" {
  name     = google_cloud_run_v2_service.api.name
  location = var.region
  role     = "roles/run.invoker"
  member   = "allUsers"
}

# Cloud Run Function (interna)
resource "google_cloud_run_v2_service" "function" {
  name                = "motor-decisao-function"
  location            = var.region
  deletion_protection = false

  template {
    containers {
      image = "gcr.io/${var.project_id}/motor-decisao-function:1.0"

      env {
        name  = "BucketOutput"
        value = google_storage_bucket.output.name
      }
    }

    service_account = google_service_account.function_sa.email
  }

  ingress = "INGRESS_TRAFFIC_INTERNAL_ONLY"
}
