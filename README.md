# [![CI/CD SaraBank](https://github.com/phillrog/desafio-sara-bank/actions/workflows/gcp-ci-cd.yml/badge.svg)](https://github.com/phillrog/desafio-sara-bank/actions/workflows/gcp-ci-cd.yml)

🏦 SARA Bank API
================

Uma API de serviços bancários de alta resiliência desenvolvida em **.NET 8**, utilizando arquitetura orientada a eventos e infraestrutura automatizada na **Google Cloud Platform (GCP)**.

🚀 Tecnologias e Bibliotecas
----------------------------

| **Biblioteca / Recurso** | **Finalidade** |
| --- | --- |
| **.NET 8** | Framework principal da aplicação. |
| **Google Cloud Firestore** | Banco de dados NoSQL e controle de *Transactional Outbox*. |
| **Google Cloud Pub/Sub** | Mensageria para processamento assíncrono e Sagas. |
| **Identity Platform (Firebase)** | Autenticação e gestão de usuários via JWT. |
| **MediatR** | Desacoplamento de Commands e Handlers. |
| **FluentValidation** | Validação de regras de negócio (ex: CPFs). |
| **Polly** | Resiliência com políticas de Retry e Circuit Breaker. |
| **xUnit / Moq / FluentAssertions** | Stack de testes unitários e linguagem ubíqua. |

* * * * *

🏗️ Infraestrutura como Código (IaC)
------------------------------------

A infraestrutura é provisionada via **Terraform**, garantindo que o ambiente seja replicável e auditável.

-   **Cloud Run:** Hospedagem da API em containers que escalam automaticamente.

-   **Artifact Registry:** Repositório privado para as imagens Docker.

-   **Firestore:** Configurado em modo Nativo com índices compostos para performance.

-   **Pub/Sub:** Tópicos e Subscriptions configurados para orquestração de Sagas de transferência.

* * * * *

## O Terraform configurou o "esqueleto" e o "coração" da infraestrutura do **SARA Bank**:

### 1\. APIs e Serviços (Os Motores)

O código ativa as permissões para que o Google permita o uso dos serviços. Sem isso, nada funciona:

-   **Firestore API:** Banco de dados.

-   **Pub/Sub API:** Mensageria/Eventos.

-   **Artifact Registry API:** Armazenamento de imagens Docker.

-   **Cloud Run API:** Hospedagem da API .NET.

-   **Identity Toolkit API:** Serviço de autenticação (Identity Platform/Firebase).

### 2\. Banco de Dados (Firestore)

-   **Instância Firestore:** Cria o banco de dados no modo **Nativo**.

-   **Configuração de Deleção:** Define a política de exclusão (`deletion_policy = "DELETE"`) para permitir limpar o banco via código.

-   **Índices Compostos:** Provisiona 3 índices essenciais para o C# não dar erro ao buscar dados complexos:

    -   `Movimentacoes`: Índice para busca por `ContaId` e data decrescente (Extrato).

    -   `Movimentacoes`: Índice para busca por `SagaId` e `Tipo` (Idempotência da Saga).

    -   `Outbox`: Índice para o Worker processar mensagens pendentes (`Processado` + `Tentativas`).

### 3\. Mensageria (Pub/Sub)

Cria toda a malha de comunicação assíncrona para a **Saga de Transferência**:

-   **Tópicos:**

    -   `sara-bank-usuarios`: Cadastro de novos clientes.

    -   `sara-bank-movimentacoes`: Registro de fluxos financeiros.

    -   `sara-bank-transferencias-iniciadas`: Gatilho da Saga (Débito).

    -   `sara-bank-transferencias-debitadas`: Próximo passo (Crédito).

    -   `sara-bank-transferencias-compensar`: Caminho de erro (Estorno).

    -   `sara-bank-transferencias-erros`: Logs de falhas críticas.

    -   `sara-bank-transferencias-concluidas`: Sucesso total.

-   **Subscriptions (Consumidores):** Cria as "filas" onde os Workers da sua API .NET ficam "escutando" para processar cada um dos tópicos acima.

### 4\. Segurança e Identidade (Identity Platform)

-   **Configuração de Auth:** Define as regras de login (E-mail e Senha ativos).

-   **Regras de Login:** Bloqueia e-mails duplicados para garantir unicidade de usuários.

-   **Authorized Domains:** Libera o `localhost` e as URLs do projeto (`firebaseapp.com`) para evitar ataques de domínios não autorizados.

### 5\. Armazenamento (Artifact Registry)

-   **Docker Repository:** Cria o repositório `sarabank-repo` na região `us-central1`. É aqui que o seu GitHub Actions guarda as imagens Docker da API antes de fazer o deploy.

🛠️ Fluxo de CI/CD (GitHub Actions)
-----------------------------------

O projeto utiliza um pipeline profissional dividido em duas grandes etapas:

1.  **Integração Contínua (CI):**

    -   Execução automática de testes unitários a cada build.

    -   Validação de sintaxe e compilação da solução.

2.  **Entrega Contínua (CD):**

    -   **Gate de Aprovação:** O deploy exige uma confirmação manual no ambiente de produção.

    -   **Dockerization:** Build e Push da imagem para o Google Artifact Registry.

    -   **Deploy:** Atualização automática do serviço no Cloud Run.

* * * * *

🔑 Configuração Local
---------------------

Para rodar o projeto localmente e conectar ao ambiente da nuvem, é necessário uma **Service Account**:

1.  Gere a chave JSON no console do GCP ou via CLI:

    Bash

    ```
    gcloud iam service-accounts keys create sara-bank-key.json\
        --iam-account=sarabank-app-sa@sara-bank.iam.gserviceaccount.com

    ```

2.  Configure o caminho da chave no seu `appsettings.Development.json` ou via variável de ambiente `GOOGLE_APPLICATION_CREDENTIALS`.
3.  Pegue o ApiKey  do Firebase e coloque no appsettings

*** Como obter a Firebase API Key

Para que a autenticação (JWT) funcione, você precisa da chave que o Google gera automaticamente:

1.  Acesse o [Console do Google Cloud](https://console.cloud.google.com/).

2.  No menu lateral, vá em **APIs e Serviços > Credenciais**.

3.  Procure na lista por **Chaves de API**.

4.  A chave que você precisa é a chamada **"Browser key (auto created by Firebase)"**.

    -   *Se ela não existir, ela aparece assim que você ativa o Identity Platform no console do Firebase.*

* * * * *

📊 Comandos de Validação (CLI)
------------------------------

Bash

```
# Listar tópicos do Pub/Sub
gcloud pubsub topics list --project=sara-bank

# Verificar índices compostos do Firestore
gcloud firestore indexes composite list --project=sara-bank
```




### 🔐 Configuração de Secrets e CI/CD

Para rodar este projeto via GitHub Actions, você não deve subir o arquivo JSON. Em vez disso, converta-o para **Base64** e salve-o nas *Secrets* do seu repositório.

#### 1\. Gerar Base64 da sua Chave (Localmente)

No terminal (Linux/Mac ou Cloud Shell), rode:

Bash

```
base64 -w 0 sara-bank-key.json > key_base64.txt

```

*(No Windows PowerShell: `[Convert]::ToBase64String([IO.File]::ReadAllBytes("sara-bank-key.json"))`)*

#### 2\. Configurar o GitHub Secrets

No seu repositório do GitHub, vá em **Settings > Secrets and variables > Actions** e adicione:

-   `GCP_SA_KEY`: Cole o conteúdo gerado (a string em Base64).

-   `GCP_PROJECT_ID`: O ID do seu projeto (ex: `sara-bank`).

-   `FIREBASE_API_KEY`: A chave de API do seu Identity Platform.

#### 3\. Como o Projeto utiliza essa Chave

O workflow do GitHub Actions (`.yml`) está configurado para decodificar essa string e autenticar automaticamente no Google Cloud antes de realizar o Build do Docker e o Deploy no Cloud Run:

YAML

```
- name: 'Authenticate to Google Cloud'
  uses: 'google-github-actions/auth@v2'
  with:
    credentials_json: '${{ secrets.GCP_SA_KEY }}'
```

# Resultado

![sara-bank](https://github.com/user-attachments/assets/4f2a5b0b-c6b1-42fc-89ae-a9b0b4eb3763)

<img width="1915" height="968" alt="Captura de tela 2026-01-10 205011" src="https://github.com/user-attachments/assets/bf93eb20-e9b9-42c9-938e-35c8cb960a3e" />

<img width="1919" height="964" alt="Captura de tela 2026-01-10 205623" src="https://github.com/user-attachments/assets/4edd1f4d-05b2-441e-8c4e-60d2c7d88a5b" />

<img width="1912" height="978" alt="Captura de tela 2026-01-10 205712" src="https://github.com/user-attachments/assets/8f1a43d5-4d8e-4baf-a50f-81f5bb6a6f5f" />

<img width="1908" height="970" alt="Captura de tela 2026-01-10 205730" src="https://github.com/user-attachments/assets/18bb4101-8e4a-4f1a-be05-c10cbce4cd3d" />

<img width="1868" height="967" alt="Captura de tela 2026-01-10 211557" src="https://github.com/user-attachments/assets/80eea99d-fa4b-4b1e-af9a-12e9d981b474" />

# Atenção

Destrua os recursos criados com terraform, em seguida o projeto para evitar custos.

<img width="1868" height="972" alt="Captura de tela 2026-01-10 213810" src="https://github.com/user-attachments/assets/6c7047b5-b2af-405f-8a4f-724212462b3a" />
