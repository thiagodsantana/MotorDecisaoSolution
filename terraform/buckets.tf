resource "google_storage_bucket" "input" {
  name                        = "motor-decisao-input"
  location                    = var.region
  force_destroy               = true
  uniform_bucket_level_access = true
}

resource "google_storage_bucket" "output" {
  name                        = "motor-decisao-output"
  location                    = var.region
  force_destroy               = true
  uniform_bucket_level_access = true
}

# Permissões Cloud Run API
resource "google_storage_bucket_iam_member" "run_sa_input" {
  bucket = google_storage_bucket.input.name
  role   = "roles/storage.objectAdmin"
  member = "serviceAccount:${google_service_account.run_sa.email}"
}

resource "google_storage_bucket_iam_member" "run_sa_output" {
  bucket = google_storage_bucket.output.name
  role   = "roles/storage.objectAdmin"
  member = "serviceAccount:${google_service_account.run_sa.email}"
}

# Permissões Cloud Run Function
resource "google_storage_bucket_iam_member" "function_sa_input" {
  bucket = google_storage_bucket.input.name
  role   = "roles/storage.objectAdmin"
  member = "serviceAccount:${google_service_account.function_sa.email}"
}

resource "google_storage_bucket_iam_member" "function_sa_output" {
  bucket = google_storage_bucket.output.name
  role   = "roles/storage.objectAdmin"
  member = "serviceAccount:${google_service_account.function_sa.email}"
}
