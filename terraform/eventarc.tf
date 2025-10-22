resource "google_eventarc_trigger" "function_trigger" {
  name     = "trigger-proposta"
  location = var.region

  matching_criteria {
    attribute = "type"
    value     = "google.cloud.storage.object.v1.finalized"
  }

  matching_criteria {
    attribute = "bucket"
    value     = google_storage_bucket.input.name
  }

  destination {
    cloud_run_service {
      service = google_cloud_run_v2_service.function.name
      region  = var.region
    }
  }

  service_account = google_service_account.function_sa.email

  depends_on = [
    google_cloud_run_v2_service.function,
    google_storage_bucket.input
  ]
}
