````markdown
# 🚀 Motor de Decisão - Cloud Run, Function + Eventarc

Este projeto demonstra uma arquitetura **ClourRun e Serverless no Google Cloud** usando:

- **Cloud Run** para expor uma API REST que recebe propostas.
- **Cloud Storage** como bucket de entrada e saída.
- **Cloud Run (Function containerizada)** para processar arquivos quando novos objetos são adicionados ao bucket de entrada.
- **Eventarc** para acionar a Function automaticamente.

---

## 📦 Estrutura

- **MotorDecisao.API** → API REST em .NET rodando no Cloud Run.
- **ProcessarProposta** → Function em .NET (container) que processa arquivos do bucket.

---

## 🚀 Deploy Passo a Passo

### 1) Build e Push da API
```bash
cd MotorDecisao.API
gcloud builds submit --tag gcr.io/motordecisao/motor-decisao-api:1.0 .
````

### 2) Deploy da API no Cloud Run

```bash
gcloud run deploy motor-decisao-api \
  --image gcr.io/motordecisao/motor-decisao-api:1.0 \
  --region us-central1 \
  --platform managed \
  --allow-unauthenticated \
  --set-env-vars BucketInput=motor-decisao-input
```

---

### 3) Teste da API (Postman ou Curl)

```bash
curl --location 'https://motor-decisao-api-272573457808.us-central1.run.app/propostas' \
--header 'Content-Type: application/json' \
--data-raw '{
  "nome": "João Silva",
  "cpf": "12345678900",
  "rendaMensal": 4200.50,
  "idade": 33,
  "telefone": "(11)99999-0000",
  "email": "joao@example.com"
}'
```

---

### 4) Build e Push da Function (containerizada)

```bash
cd ProcessarProposta
gcloud builds submit --tag gcr.io/motordecisao/motor-decisao-function:1.0 .
```

### 5) Deploy da Function no Cloud Run

```bash
gcloud run deploy motor-decisao-function \
  --image gcr.io/motordecisao/motor-decisao-function:1.0 \
  --region us-central1 \
  --platform managed \
  --no-allow-unauthenticated \
  --set-env-vars BucketOutput=motor-decisao-output
```

> Alternativa: Deploy direto a partir do código

```bash
gcloud beta run deploy motor-decisao-function \
  --source . \
  --function ProcessarProposta.Function \
  --region us-central1 \
  --base-image dotnet8 \
  --no-allow-unauthenticated
```

---

### 6) Criar Trigger Eventarc

```bash
gcloud eventarc triggers create motor-decisao-function-trigger \
  --location=us-central1 \
  --destination-run-service=motor-decisao-function \
  --destination-run-region=us-central1 \
  --event-filters="type=google.cloud.storage.object.v1.finalized" \
  --event-filters="bucket=motor-decisao-input" \
  --service-account=272573457808-compute@developer.gserviceaccount.com
```

---

## 🔗 Consoles GCP

* [Projeto GCP](https://console.cloud.google.com/run?project=motordecisao)
* [API no Cloud Run](https://console.cloud.google.com/run/detail/us-central1/motor-decisao-api/observability/metrics?project=motordecisao)
* [Function no Cloud Run](https://console.cloud.google.com/run/detail/us-central1/motor-decisao-function/observability/metrics?project=motordecisao)
* [Bucket de Entrada](https://console.cloud.google.com/storage/browser/motor-decisao-input/applications?hl=pt-br&project=motordecisao)

---

## ✅ Fluxo Completo

1. API recebe a **proposta** e gera um arquivo JSON no bucket `motor-decisao-input`.
2. O **Eventarc** detecta a criação do arquivo e aciona a Function.
3. A **Function** processa o arquivo e grava o resultado no bucket `motor-decisao-output`.

---

```
```
