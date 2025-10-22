Perfeito! Aqui está uma versão **otimizada em Markdown** do README, pronta para GitHub, incluindo **badges de Terraform, GCP e status de deploy**:

```markdown
# Motor de Decisão de Crédito – Infraestrutura GCP com Terraform

[![Terraform](https://img.shields.io/badge/Terraform-v1.8.0-blue)](https://www.terraform.io/)
[![GCP](https://img.shields.io/badge/GCP-Cloud_Run-red)](https://cloud.google.com/run)
[![Status](https://img.shields.io/badge/Status-Deploy%20Completo-green)](#)

Infraestrutura para o **Motor de Decisão de Crédito** na **Google Cloud Platform**, utilizando:

- **Cloud Run**: API pública e Function interna.
- **Cloud Storage**: Buckets de entrada e saída.
- **Eventarc**: Trigger para processar arquivos JSON.
- **Terraform**: IaC modular para criar e gerenciar todos os recursos.

---

## Índice

- [Pré-requisitos](#pré-requisitos)
- [Estrutura do projeto](#estrutura-do-projeto)
- [Configuração](#configuração)
- [Deploy](#deploy)
- [Arquitetura](#arquitetura)
- [Testes](#testes)
- [URLs importantes](#urls-importantes)
- [Manutenção](#manutenção)

---

## Pré-requisitos

- Conta GCP com projeto ativo
- Terraform >= 1.8.0
- GCloud SDK autenticado
- Permissões de Admin em Cloud Run, IAM e Storage
- .NET 8 SDK
- Docker

---

## Estrutura do projeto

```

terraform/
├─ main.tf
├─ variables.tf
├─ service_accounts.tf
├─ buckets.tf
├─ cloud_run.tf
├─ eventarc.tf
├─ iam.tf
├─ outputs.tf

````

---

## Configuração

1. Clone o repositório:

```bash
git clone <repo-url>
cd terraform
````

2. Inicialize o Terraform:

```bash
terraform init
```

3. Veja o plano de deploy:

```bash
terraform plan
```

4. Ajuste variáveis se necessário no `variables.tf`:

```hcl
variable "project_id" { default = "motordecisao" }
variable "region" { default = "us-central1" }
```

---

## Deploy

1. **Deploy de toda a infraestrutura com Terraform**:

```bash
terraform apply -auto-approve
```

2. **Build e deploy da API Cloud Run**:

```bash
cd MotorDecisao.API
gcloud builds submit --tag gcr.io/motordecisao/motor-decisao-api:1.0 .
gcloud run deploy motor-decisao-api \
  --image gcr.io/motordecisao/motor-decisao-api:1.0 \
  --region us-central1 \
  --platform managed \
  --allow-unauthenticated \
  --set-env-vars BucketInput=motor-decisao-input,BucketOutput=motor-decisao-output
```

3. **Build e deploy da Function Cloud Run (interna)**:

```bash
cd ProcessarProposta
gcloud builds submit --tag gcr.io/motordecisao/motor-decisao-function:1.0 .
gcloud run deploy motor-decisao-function \
  --image gcr.io/motordecisao/motor-decisao-function:1.0 \
  --region us-central1 \
  --platform managed \
  --no-allow-unauthenticated \
  --set-env-vars BucketOutput=motor-decisao-output
```

Eventarc será criado pelo Terraform para acionar a Function quando arquivos JSON forem enviados ao bucket de input.

---

## Arquitetura

* **Cloud Run API (`motor-decisao-api`)**

  * Recebe propostas via POST.
  * Salva no bucket de input `motor-decisao-input`.
  * Público (`allow-unauthenticated`).

* **Cloud Run Function (`motor-decisao-function`)**

  * Lê arquivos JSON do bucket de input.
  * Gera decisão e salva no bucket de output `motor-decisao-output`.
  * Interno (`INGRESS_TRAFFIC_INTERNAL_ONLY`).

* **Buckets**

  * `motor-decisao-input`
  * `motor-decisao-output`

* **Eventarc**

  * Trigger acionando Function ao detectar novos arquivos finalizados no bucket de input.

---

## Testes

### API

```bash
curl --location 'https://<cloud_run_api_url>/propostas' \
--header 'Content-Type: application/json' \
--data-raw '{
    "nome": "João Silva",
    "cpf": "12345678900",
    "rendaMensal": 4200,
    "idade": 33,
    "telefone": "(11)99999-0000",
    "email": "joao@example.com"
}'
```

* Arquivo salvo no bucket de input.
* Function acionada automaticamente.

---

## URLs importantes

* [Projeto GCP](https://console.cloud.google.com/home/dashboard?project=motordecisao)
* [Cloud Run API](https://console.cloud.google.com/run/detail/us-central1/motor-decisao-api/overview?project=motordecisao)
* [Cloud Run Function](https://console.cloud.google.com/run/detail/us-central1/motor-decisao-function/overview?project=motordecisao)
* [Bucket Input](https://console.cloud.google.com/storage/browser/motor-decisao-input?project=motordecisao)
* [Bucket Output](https://console.cloud.google.com/storage/browser/motor-decisao-output?project=motordecisao)

---

## Manutenção

* Atualizar containers: build + push + `gcloud run deploy`.
* Atualizar infra: modificar Terraform + `terraform apply`.
* Monitoramento: Cloud Run Logs e métricas.
* Debug Function: Eventarc + logs do Cloud Run interno.

---

```
Quer que eu faça essa versão também?
```
