output "cloud_run_api_url" {
  value       = google_cloud_run_v2_service.api.uri
  description = "URL pública da API Cloud Run"
}

output "cloud_run_function_url" {
  value       = google_cloud_run_v2_service.function.uri
  description = "URL da Function Cloud Run (acesso interno)"
}

output "bucket_input_url" {
  value = "https://storage.googleapis.com/${google_storage_bucket.input.name}/"
}

output "bucket_output_url" {
  value = "https://storage.googleapis.com/${google_storage_bucket.output.name}/"
}
