resource "google_service_account" "run_sa" {
  account_id   = "motor-decisao-run-sa"
  display_name = "Service Account - Cloud Run API"
}

resource "google_service_account" "function_sa" {
  account_id   = "motor-decisao-function-sa"
  display_name = "Service Account - Cloud Run Function"
}
