# Order Management API

API REST para gerenciamento de pedidos desenvolvida em ASP.NET Core com Entity Framework Core e SQL Server.

## Stack utilizada

- .NET 10
- ASP.NET Core Web API
- Entity Framework Core
- SQL Server
- Docker Compose
- Swagger

---

## Funcionalidades

- Criar pedidos
- Listar pedidos com paginação
- Buscar pedido por ID
- Cancelar pedidos
- Persistência em banco relacional
- Middleware global de exceções
- Swagger configurado

---

## Estrutura do projeto

```text
Features/
  Orders/
    Controllers/
    DTOs/
    Services/

Shared/
  Data/
  Exceptions/
  Middleware/
  Models/
```

A organização foi feita por feature para manter arquivos relacionados próximos e evitar separações excessivas para um projeto pequeno.

---

## Como executar

### Subir a aplicação

```bash
docker compose up --build
```

A API ficará disponível em:

```text
http://localhost:8080
```

---

## Swagger

```text
http://localhost:8080/swagger
```

---

## Migrations

### Criar migration

```bash
dotnet ef migrations add NomeDaMigration
```

### Aplicar migrations

```bash
dotnet ef database update
```

---

## Endpoints principais

### Criar pedido

```http
POST /orders
```

### Listar pedidos

```http
GET /orders?page=1&pageSize=10
```

### Buscar pedido por ID

```http
GET /orders/{id}
```

### Cancelar pedido

```http
PUT /orders/{id}/cancel
```

---

## Observações

- O valor total do pedido é calculado pela API.
- O endpoint de listagem retorna uma versão resumida dos pedidos.
- O endpoint de detalhes retorna os itens do pedido.
- As entidades do EF Core não são expostas diretamente pela API.
- O tratamento de exceções foi centralizado em middleware.
- O projeto foi mantido propositalmente simples, evitando abstrações desnecessárias para o escopo do teste.