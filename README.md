# TrueReplayer

**TrueReplayer** permite **gravar** e **reproduzir** ações do teclado, mouse e scroll automaticamente. Ideal para automações simples, testes repetitivos ou automação de tarefas no PC.

---

![red1](https://github.com/user-attachments/assets/105f17fc-d752-4bbe-8d02-0b5bdb6d3f43)  ![red2](https://github.com/user-attachments/assets/a8d5e523-1089-4acc-b3fe-2e770826a0d2)

![red3](https://github.com/user-attachments/assets/df8a7415-6ada-4263-bf9d-b83c4ccb568b)  ![red4](https://github.com/user-attachments/assets/89423b94-5e59-4c3c-8b3c-7349203e0801)

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
  - Configure hotkeys para iniciar/pausar gravação (`default: F9`) e reprodução (`default: F10`).

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
   - Clique no botão **Record** ou pressione a hotkey configurada (`F9`).
   - Realize as ações desejadas (cliques, rolagem, teclado).
   - Pressione **Record** novamente para parar.

2. **Reproduzindo Ações**
   - Após gravar, clique no botão **Replay** ou pressione a hotkey (`F10`).
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
