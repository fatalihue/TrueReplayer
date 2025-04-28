# TrueReplayer

**TrueReplayer** é um aplicativo Windows desenvolvido com **WinUI 3** que permite **gravar** e **reproduzir** ações do teclado, mouse e scroll automaticamente. Ideal para automações simples, testes repetitivos ou automação de tarefas no PC.

---

![TrueReplayer_tndTBKp0rF](https://github.com/user-attachments/assets/252bee5b-2240-4076-bb5f-6e3db1372186)


---

## 🎯 Funcionalidades

- **Gravação de Ações**:
  - Captura movimentos e cliques do mouse (Left, Right e Middle Click - Down/Up).
  - Captura eventos de rolagem (Scroll Up/Down).
  - Captura teclas pressionadas (KeyDown/KeyUp).

- **Reprodução Automática**:
  - Reproduz as ações gravadas fielmente.
  - Suporte a **loops** automáticos com:
    - Número de repetições configurável.
    - Intervalo entre as repetições.

- **Gerenciamento de Perfis**:
  - Salve, carregue e gerencie perfis de gravações.
  - Perfis armazenados em `Documentos/TrueReplayerProfiles`.

- **Atalhos Personalizados**:
  - Configure hotkeys para iniciar/pausar gravação (`default: F2`) e reprodução (`default: F3`).

- **Customização**:
  - Defina delays personalizados entre ações.
  - Habilite/Desabilite gravação específica de mouse, teclado e scroll.

- **Outros Recursos**:
  - Janela sempre no topo (Always on Top).
  - Minimizar para bandeja do sistema.
  - Interface moderna com suporte a temas claros e escuros.
  - Copiar ações para área de transferência.

---

## 🚀 Como Usar

1. **Gravando Ações**
   - Abra o aplicativo.
   - Clique no botão **Record** ou pressione a hotkey configurada (`F2`).
   - Realize as ações desejadas (cliques, rolagem, teclado).
   - Pressione **Record** novamente para parar.

2. **Reproduzindo Ações**
   - Após gravar, clique no botão **Replay** ou pressione a hotkey (`F3`).
   - Configure se desejar usar loop e o número de repetições.

3. **Gerenciando Perfis**
   - Use os botões **Save**, **Load**, **Reset** para gerenciar suas ações gravadas como perfis.

4. **Configurações Úteis**
   - Defina delays personalizados no campo **Custom Delay**.
   - Ajuste o número de loops e intervalo entre repetições.
   - Habilite "Always on Top" ou "Minimize to Tray" conforme desejado.

---

## 📋 Notas

- Perfis são salvos automaticamente no diretório `Documentos/TrueReplayerProfiles`.
- O programa registra hooks globais de teclado e mouse, portanto, dependendo das configurações do Windows, pode ser necessário rodar o programa como administrador para capturar eventos corretamente.

---

## 🧹 Principais Classes e Serviços

| Componente | Função |
|:-----------|:-------|
| `ActionRecorder` | Grava eventos de teclado, mouse e scroll. |
| `ReplayService` | Reproduz a sequência de ações gravadas. |
| `MainController` | Coordena as ações principais (gravação, reprodução, UI). |
| `HotkeyManager` | Gerencia os atalhos de gravação e reprodução. |
| `ProfileController` | Salva e carrega perfis de ações em arquivos `.json`. |
| `TrayIconService` | Gerencia o ícone e contexto da bandeja do sistema. |
| `DelayManager` | Gerencia delays personalizados entre ações. |
| `LoopControlManager` | Gerencia parâmetros de loop nas reproduções. |

---

## 🛠️ Tecnologias Utilizadas

- **C#**
- **WinUI 3**
- **Windows App SDK**
- **CommunityToolkit.WinUI.UI.Controls**

---

## 📂 Estrutura do Projeto

```plaintext
├── MainWindow.xaml            # Interface principal
├── MainWindow.xaml.cs          # Lógica da janela principal
├── Controllers/                # Controle da aplicação (MainController, HotkeyManager, etc.)
├── Services/                   # Serviços auxiliares (ReplayService, TrayIconService, etc.)
├── Models/                     # Modelos de dados (ActionItem, UserProfile)
├── Interop/                    # Métodos nativos Win32 (Hooks, Window Management)
├── Helpers/                    # Utilitários diversos
└── Assets/                     # Ícones e recursos visuais
```

---
