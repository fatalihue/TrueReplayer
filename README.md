# TrueReplayer

**TrueReplayer** permite **gravar** e **reproduzir** ações do teclado, mouse e scroll automaticamente. Ideal para automações simples, testes repetitivos ou automação de tarefas no PC.

---

![TRapp1red](https://github.com/user-attachments/assets/3a380118-853f-437a-9aaa-118026519ca4)  ![TRapp2red](https://github.com/user-attachments/assets/8c696b9f-b592-414e-9f22-bda05bfbe605)

---

![TRapp3red](https://github.com/user-attachments/assets/d36fd98c-bdc6-449e-aa7c-ffd29302aa13)  ![TRapp4red](https://github.com/user-attachments/assets/fa38e165-286b-4712-8a8f-7ee28a65faed)

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
