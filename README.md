# 🎮 TrueReplayer

**TrueReplayer** é um aplicativo desktop avançado (WinUI 3) que grava e reproduz ações do usuário (como cliques do mouse, teclas pressionadas e rolagem), com suporte a hotkeys, delays personalizados, perfis salvos e controle de repetição. Ideal para automações, testes repetitivos e criação de macros personalizadas.

---

![img1](https://github.com/user-attachments/assets/edde7e9a-ab76-42ea-8403-4c3a856505c2)
![img3](https://github.com/user-attachments/assets/93423987-a358-486e-b284-b9aeeb43a707)
![img4](https://github.com/user-attachments/assets/4f4007c9-bb63-4c26-a778-7be4a71de34f)

---

## 🧠 Funcionalidades

- ✅ Gravação de ações do usuário:
  - Cliques do mouse (botão esquerdo, direito, meio)
  - Roda do mouse (scroll up/down)
  - Teclado (teclas pressionadas e liberadas)
- 🔁 Reprodução com:
  - Delay configurável entre ações
  - Loop infinito ou com contagem limitada
  - Intervalo entre repetições
- 📝 Inserção de ações no meio da lista
- 🗂 Perfis salvos no formato JSON
- 🎛 Interface moderna com controles visuais
- 🔒 Hotkeys personalizadas para gravar e reproduzir
- 📋 Copiar ações em formato de tabela para a área de transferência

---

## 🖥 Interface do Usuário

- Botões: **Recording**, **Replay**, **Clear**, **Copy**
- Grid com as ações gravadas: exibe tipo, coordenadas, delay e comentário
- Campos de controle:
  - Hotkey de gravação (`F9` por padrão)
  - Hotkey de reprodução (`F10` por padrão)
  - Delay personalizado (opcional)
  - Repetição (loop): quantidade e intervalo entre repetições
- Switches:
  - 🎯 Gravar mouse / scroll / teclado
  - ⏫ Always on Top
  - 📥 Minimize to Tray

---

## 🚀 Como Usar

### 1. Iniciar a aplicação

Compilar com suporte a WinUI 3 (.NET 6+). Certifique-se de ter o SDK apropriado instalado. Execute o `MainWindow.xaml.cs`.

---

### 2. Gravar ações

- Clique em `Recording` ou pressione a hotkey (F9)
- Execute ações com o mouse e teclado
- Clique novamente em `Recording` para parar

---

### 3. Reproduzir ações

- Clique em `Replay` ou pressione a hotkey (F10)
- Configure o número de repetições e intervalo, se necessário

---

### 4. Inserir ações no meio

- Selecione a linha no grid onde deseja inserir
- Pressione a hotkey de gravação
- As novas ações serão inseridas logo após a linha marcada

---

### 5. Salvar / carregar perfis

- Use os botões `Save` e `Load` para exportar/importar perfis no formato JSON
- Perfis ficam salvos em `Documentos/TrueReplayerProfiles`

---

## 📦 Requisitos

- Windows 10 ou 11
- [.NET 6.0+](https://dotnet.microsoft.com/en-us/download)
- SDK do Windows App SDK com suporte a WinUI 3

---

## 🛡 Avisos

- A automação é local: nenhuma ação é enviada pela internet.
- Evite uso para atividades que violem os termos de serviço de softwares de terceiros.

---

## 🧑‍💻 Contribuição

Pull Requests e sugestões são bem-vindos! Se você encontrar bugs ou tiver ideias de melhorias, abra uma issue.

---

