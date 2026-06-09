# 🏭 SKAI - Simulador Kaizen de Aprendizado Imersivo

**SKAI** é um simulador industrial interativo desenvolvido em Unity para o aprendizado prático e imersivo dos conceitos de **Lean Manufacturing (Manufatura Enxuta)**. 

O simulador foi projetado para estudantes, engenheiros de produção e profissionais da indústria vivenciarem na prática os impactos das metodologias enxutas em uma linha de produção ativa, oferecendo uma percepção visual e física que apresentações e livros tradicionais não conseguem proporcionar.

---

## 🎯 Proposta de Valor
Em vez de apenas memorizar conceitos teóricos, o usuário é colocado no chão de fábrica em tempo real. Ele pode observar o fluxo físico das peças, identificar gargalos, conversar com operários (NPCs), resolver falhas e visualizar instantaneamente o reflexo de suas ações em um dashboard de KPIs dinâmico.

---

## 🛠️ Desafios Kaizen Implementados

1. **Desafio TPM (Manutenção Produtiva Total):**
   * **Simulação:** Máquinas quebram aleatoriamente, interrompendo o fluxo produtivo.
   * **Mecânica Imersiva:** As peças físicas se acumulam na esteira (sistema de fila FIFO real com física de colisões). O jogador deve ir até a máquina quebrada (sinalizada em vermelho) e realizar o reparo.

2. **Desafio Qualidade Jidoka (Automação com Toque Humano):**
   * **Simulação:** Máquinas descalibradas produzem peças defeituosas (representadas visualmente como esferas brilhantes em vermelho emissivo).
   * **Mecânica Imersiva (Jidoka Stop):** Ao detectar o primeiro defeito na fonte, a máquina aciona um parada automática de linha (*Line Stop*), acende uma luz amarela/vermelha e bloqueia a entrada de novas peças para evitar a propagação do erro. O jogador deve falar com o operador, recalibrar a máquina e restabelecer o fluxo para alcançar o objetivo de **5 peças boas consecutivas**.

3. **Desafio de Balanceamento de Linha:**
   * **Simulação:** Diferenças nos tempos de ciclo entre estações sequenciais (ex: Torno vs Fresadora) causam gargalos de inventário.
   * **Mecânica Imersiva:** O jogador analisa visualmente qual esteira está sobrecarregada, conversa com os operários para entender os tempos e ajusta os parâmetros para equilibrar a capacidade produtiva e otimizar o Lead Time.

---

## 📊 Perfis de Produção Customizáveis
O simulador suporta alteração dinâmica do perfil industrial:
* **Metalúrgica:** Matérias-primas e produtos são estilizados visualmente como **esferas de aço prata polido**.
* **Eletrônica SMD:** Matérias-primas e produtos são estilizados visualmente como **esferas de plástico verde (placas de circuito/PCB)**.

---

## 🕹️ Controles e Atalhos

| Comando | Ação |
| :--- | :--- |
| **W, A, S, D** / **Setas** | Movimentação pelo chão de fábrica |
| **Botão Direito do Mouse (Segurar) + Mouse** | Olhar ao redor (Rotação da câmera livre) |
| **E** / **Q** | Subir / Descer a altura dos olhos (Ajuste de visão para PC) |
| **Clique Esquerdo do Mouse** | Interagir (Clicar nos botões do Canvas, conversar com NPCs, consertar/recalibrar máquinas) |
| **H** | **Teleporte de Retorno:** Retorna instantaneamente para a Sala de Estudos, em frente ao telão principal |
| **Espaço** | Pular |
| **ESC** | Destravar/Exibir o cursor do mouse |

---

## 💻 Build Funcional (Executável Desktop)
O projeto já conta com uma build funcional pré-compilada para Windows Standalone (64-bits). Para testar a simulação diretamente sem precisar abrir o editor da Unity:
1. Acesse a pasta `Builds/` na raiz do projeto.
2. Execute o arquivo **`SKAI_Simulador.exe`**.
3. O simulador abrirá em janela/tela cheia com suporte total aos controles listados acima.

---

## 🚀 Como Executar o Projeto via Unity Editor

1. Abra a pasta do projeto no **Unity Editor** (versão recomendada: Unity com suporte a HDRP/URP).
2. Abra a cena principal localizada em:  
   `Assets/UnityFactorySceneHDRP/Scene_Factory/FactorySceneSample.unity`
3. Certifique-se de que o **Play Mode** está desativado.
4. No menu superior da Unity, clique em:  
   **`SKAI > Fix and Setup Everything (Consertar Tudo)`**  
   *(Este comando automatizado limpa objetos duplicados, reconecta splines de animação de personagens, vincula NPCs e monta os Canvas de UI dinamicamente).*
5. Pressione **Play** no Unity Editor para iniciar a experiência!

---

## 📁 Estrutura de Pastas de Scripts Criada

Toda a lógica da simulação está organizada em scripts modulares em `Assets/Scripts/`:
* `Item.cs`: Gerencia as propriedades físicas, tags e renderização dinâmica de materiais (aço, PCB, refugo) das peças.
* `Workstation.cs`: Controla o ciclo de processamento, cálculo de atividade, Jidoka stops e acionamento dinâmico das barreiras de contenção física de filas.
* `DialogueCanvasManager.cs` & `NPCWorker.cs`: Controlam o sistema imersivo de balões de fala em espaço 3D dos operadores.
* `KPIManager.cs` & `UIManager.cs`: Rastreiam produtividade, lead time médio, yield de qualidade e exibem no Canvas estilo Dashboard.
* `DisasterManager.cs`: Gerencia as regras de vitória, taxas de quebra e controle de cada cenário.
* `ScenarioUIController.cs`: Responde aos cliques nos botões do telão e aplica destaque visual dos modos.
* `PG_Player.cs` & `PG_FirstPersonCamera.cs`: Gerenciam a física de movimentação e visualização em primeira pessoa do usuário, com suporte para teleporte integrado.
