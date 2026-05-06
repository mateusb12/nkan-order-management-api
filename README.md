# Order Management API

API REST para gerenciamento de pedidos desenvolvida em ASP.NET Core com Entity Framework Core e SQL Server.

A ideia do projeto foi manter uma solução simples, mas organizada: endpoints funcionando, persistência real, validações básicas, Swagger, migrations e uma estrutura fácil de navegar.

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
- Testes unitários para as regras principais de pedidos

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

Tests/
  OrderManagement.Tests/
```

A organização foi feita por feature porque os arquivos relacionados ficam próximos.

No fluxo normal de uma API, quando aparece um bug em uma regra de pedidos, normalmente o caminho passa por controller, service e banco. Então, para esse escopo, faz mais sentido deixar o que é de Orders agrupado dentro de `Features/Orders` em vez de espalhar tudo em várias pastas genéricas.

---

## Como executar

### Subir a aplicação

```bash
docker compose up --build
```

Na inicialização, a API aguarda o SQL Server ficar disponível e aplica automaticamente as migrations pendentes.

A API fica disponível em:

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

## Testes

Para rodar os testes:

```bash
dotnet test
```

Os testes cobrem os principais comportamentos esperados para pedidos:

- criar pedido com total calculado pela API
- persistir pedido no banco
- não permitir pedido sem cliente
- não permitir pedido sem itens
- não permitir item com quantidade zero
- não permitir item com preço unitário zero
- listar pedidos com paginação
- buscar pedido por ID com seus itens
- retornar erro quando o pedido não existe
- cancelar pedido
- não permitir cancelar um pedido já cancelado

A ideia dos testes é validar comportamento: dada uma entrada X, o sistema deve responder com Y.  
Não tentei testar detalhes internos desnecessários, e sim as regras que fazem diferença para o funcionamento da API.

---

## Endpoints principais

### Criar pedido

```http
POST /orders
```

Exemplo:

```json
{
  "customerName": "Maria Silva",
  "items": [
    {
      "productId": 1,
      "productName": "Notebook",
      "quantity": 1,
      "unitPrice": 3500.00
    },
    {
      "productId": 2,
      "productName": "Mouse",
      "quantity": 2,
      "unitPrice": 80.00
    }
  ]
}
```

---

### Listar pedidos

```http
GET /orders?page=1&pageSize=10
```

Esse endpoint retorna uma versão resumida dos pedidos.

---

### Buscar pedido por ID

```http
GET /orders/{id}
```

Esse endpoint retorna o pedido com seus itens.

---

### Cancelar pedido

```http
PUT /orders/{id}/cancel
```

---

## Decisões técnicas

### Organização por feature

Escolhi organizar por feature porque facilita navegar pelo código.

Para um projeto desse tamanho, separar tudo em pastas genéricas como `Controllers`, `Services`, `Repositories`, `Models` etc. pode acabar espalhando arquivos que mudam juntos. Em vez disso, deixei os arquivos de pedidos próximos entre si.

Isso ajuda principalmente na manutenção: quando algo quebra em Orders, o caminho natural de investigação fica concentrado em um lugar só.

---

### Controller, service e banco

A separação principal ficou assim:

- o controller lida com HTTP
- o service concentra a lógica de pedidos
- o Entity Framework faz a interação com o banco

O controller não deveria carregar regra de negócio demais. Ele recebe a requisição, chama o service e devolve a resposta HTTP.

As regras como cálculo do total, criação do pedido, paginação, busca por ID e cancelamento ficam no `OrderService`.

---

### DTOs

Usei DTOs para deixar o contrato da API mais claro.

As entidades do Entity Framework representam a persistência. Já os DTOs representam o que entra e sai da API.

Isso evita expor diretamente o modelo do banco e também deixa mais fácil entender o formato das respostas, inclusive durante debug, testes e leitura do Swagger.

Também separei a resposta de listagem da resposta de detalhes:

- `GET /orders` retorna um resumo
- `GET /orders/{id}` retorna o pedido completo com itens

Assim a listagem não carrega nem envia dados desnecessários.

---

### Middleware global de exceções

Comecei com tratamento de erro direto no controller, mas isso rapidamente gera repetição de `try/catch`.

Por isso movi o tratamento para um middleware global.

Além de deixar os controllers mais limpos, isso cria um ponto único para observar erros da API. Se acontecer um erro inesperado no container, fica mais fácil colocar um breakpoint ou log no middleware e entender o que aconteceu.

---

### Repository Pattern

Não usei Repository Pattern porque, nesse contexto, ele adicionaria uma abstração extra sem um ganho claro.

O Entity Framework já fornece uma boa abstração para acesso a dados. Criar repositories só para encapsular chamadas simples acabaria aumentando a quantidade de arquivos e código sem resolver um problema real do projeto.

Também não considerei a troca de banco como uma justificativa suficiente para adicionar essa camada agora. Trocar banco de dados é uma decisão grande e relativamente rara, especialmente em um teste desse tamanho.

---

### SQL Server com Docker Compose

Usei SQL Server via Docker Compose para facilitar a execução do projeto.

A ideia é evitar que quem for testar precise instalar e configurar SQL Server manualmente. Com Docker, basta rodar o compose e a API já sobe junto com o banco.

---

### Dapper e RabbitMQ

#TODO

---

## Observações

- O valor total do pedido é calculado pela API.
- Pedidos possuem status.
- Não é permitido cancelar um pedido já cancelado.
- O projeto foi mantido propositalmente simples, evitando abstrações desnecessárias para o escopo do teste.