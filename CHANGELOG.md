# Changelog

Todas as alterações notáveis neste projeto serão documentadas neste arquivo.

O formato é baseado no [Keep a Changelog](https://keepachangelog.com/pt-BR/1.0.0/)
e este projeto adere ao [Semantic Versioning](https://semver.org/lang/pt-BR/).

---

## [0.4.0] - 2026-09-22

### Adicionado
- **Bolsa de Tesouro do Boss (`ThornStormBossBag`):**
  - Item consumível de Boss Bag obtido ao derrotar o Olho da Tempestade de Espinhos.
  - Tamanho compacto ao cair no chão do mundo (`24x24` hitbox, sprite proporcional `32x32`).
  - Funcionalidade completa de bolsa do tesouro vanilla: abertura via clique com o botão direito no inventário.
  - Drop garantido de **1 Moeda de Platina** e **50 Moedas de Ouro**, além dos itens da montaria do dragão e chance de obter o troféu do boss.
- **Troféu do Boss (`ThornStormBossTrophy` & `ThornStormBossTrophyTile`):**
  - Troféu de parede 3×3 (`54x54` px) fixável em paredes de fundo, conforme o padrão de troféus de bosses do Terraria vanilla.
  - Dropado pelo boss com 10% de chance direta ou obtido dentro da Bolsa de Tesouro.
- **Mini Tornados Destruíveis (`ThornMinionTornado`):**
  - Mobs hostis voadores invocados pelo boss, substituindo os antigos dragões de espinhos.
  - Reutilizam a identidade visual de tempestade do `ProjectileID.Tempest` (6 frames de animação fluida).
  - Possuem barra de vida, defesa, reagem a ataques do jogador e podem ser totalmente destruídos.
  - Escalonamento progressivo de quantidade e velocidade de perseguição conforme as fases do boss.
- **Barra de Vida Oficial do Boss (`ThornStormBossBar`):**
  - Implementação de `ModBossBar` com exibição no rodapé da tela acompanhada do nome oficial e ícone de cabeça de boss (`ThornStormBoss_Head_Boss.png`).
- **Som Característico de Invocação de Boss (`SoundID.Roar`):**
  - Rugido clássico e inconfundível de boss invocado disparado tanto no evento quanto na inicialização da IA do boss.

### Aprimorado
- **Escala e Tamanho do Boss Final (`ThornStormBoss`):**
  - Hitbox ampliada de `170x170` para `240x240`.
  - Escala visual no `PreDraw` aumentada para `2.05f + pulso` (~340px de diâmetro na tela), tornando o boss verdadeiramente colossal e intimidador.
- **Fluidez Visual e Animação Orgânica:**
  - Ciclo de animação dos 8 frames acelerado e suavizado (de 2 a 4 ticks por frame dependendo da fase) para eliminar a sensação de travamento.
  - Inclinação natural (*velocity tilt*) respondendo dinamicamente à velocidade horizontal no ar.
  - Flutuação vertical senoidal orgânica e interpolação de sombras e afterimages.
- **Progressão das 3 Fases de Combate:**
  - **Fase 1 (100%–70% HP):** Perseguição suave e cadenciada, cortina controlada de 2 espinhos e no máximo 2 mini tornados.
  - **Fase 2 (70%–40% HP):** Perseguição ágil, rajadas diagonais cruzadas de espinhos, até 4 mini tornados e pequenos dashes aéreos telegrafados a cada 5s.
  - **Fase 3 (< 40% HP):** Movimento frenético, disparos de espinhos radiais 360° combinados com leques intensos, enxame de até 7 mini tornados velozes e dashes de alta velocidade a cada 2.6s.

### Corrigido
- **Orientação e Direção de Virada da Montaria do Dragão (`DragonMount`):**
  - Corrigida a inversão de direção da cabeça do dragão ao virar: spritesheets de montarias no Terraria devem ficar voltadas para a **esquerda** por padrão. Quando o jogador olha para a direita, a engine aplica `SpriteEffects.FlipHorizontally` automaticamente. A folha de sprites foi regenerada com a cabeça voltada para a esquerda, garantindo sincronia total com a direção do jogador.
  - Adicionado controle explícito no método `Draw` assegurando que `spriteEffects` acompanhe infalivelmente `drawPlayer.direction`.
- **Estabilização Vertical e Eliminação do Clipping do Jogador no Dragão:**
  - Corrigido o mergulho vertical excessivo que fazia as pernas do jogador atravessarem o corpo do dragão para baixo.
  - Alinhados e centralizados os 9 quadros com base no centro de gravidade e dorso/sela do dragão, preservando um movimento senoidal suave e orgânico de ±2px de flutuação em voo.
  - Recalibrados `playerYOffsets = { 0, 1, 2, 3, 4, 3, 2, 1, 0 }`, `xOffset = -28` e `yOffset = 2`, mantendo o jogador perfeitamente apoiado sobre a sela com as pernas 5 a 9px acima da barriga do dragão.
- **Tamanho e Proporção do Item da Adaga (`DragonPower`):**
  - Redimensionada a textura do item de `370x306` para o padrão vanilla de `24x24` pixels ([DragonPower.png](file:///Users/dhephersonribeiro/Library/Application%20Support/Terraria/tModLoader/ModSources/O2ThornRain/Content/Items/DragonPower.png)).
  - Ajustadas as propriedades `Item.width = 24`, `Item.height = 24` e `Item.scale = 1f`, garantindo dimensões normais e idênticas a blocos e itens clássicos do Terraria tanto no inventário quanto na mão do personagem e no chão do mundo.
- **Fluxo Exclusivo de Recompensas na Bolsa de Tesouro (`ThornStormBossBag`):**
  - Removido o drop direto de `DragonPower` e `DragonMountItem` ao chão na morte do boss em `ThornStormBoss.cs`.
  - A Adaga de Espinhos agora é concedida **exclusivamente após abrir a bolsa de tesouro**.
  - O Troféu do Boss (`ThornStormBossTrophy`) agora é um drop **100% garantido** na abertura da bolsa de tesouro.

---

## [0.3.0] - 2026-09-22

### Adicionado
- **Item de Invocação do Evento (`LightningRod` - Para-raios da Tempestade):**
  - Item consumível que permite invocar "A Tempestade dos Quatro" a qualquer momento, desde que o evento não esteja ativo.
  - Arte dedicada e recortada a partir de `lightning-rod.png`.
  - Suporte completo a rede (multiplayer) com validação e sincronização autoritativa pelo servidor.
  - Receita acessível para testes (1x Bloco de Terra -> 5x Para-raios).
  - Distribuição automática de 20x unidades no inventário ao entrar no mundo ou ao criar um novo personagem para facilitar testes.
- **Arte da Adaga de Espinhos (`DragonPower`):**
  - Integração do sprite dedicado a partir de `spike-dagger.png`, substituindo o antigo fallback de Scaly Truffle.
  - Textos e descrições localizados em pt-BR e en-US como "Adaga de Espinhos" / "Spike Dagger".
- **Sprites e Animações das 3 Fases do Boss Final (`ThornStormBoss`):**
  - Conversão das artes originais em grade 4×2 para folhas de sprites verticais padronizadas de 8 frames (168×1344 px) para cada fase:
    - Fase 1: `ThornStormBoss.png`
    - Fase 2: `ThornStormBoss_Phase2.png`
    - Fase 3: `ThornStormBoss_Phase3.png`
  - Animação cíclica fluida com velocidade dinâmica adaptada à agressividade de cada fase.
  - Renderização customizada em `PreDraw` com auras pulsantes, trilhas de sombras/afterimages em alta velocidade e alinhamento preciso do centro geométrico do olho do furacão.
- **Montaria do Dragão do Cultista (`DragonMount` & `DragonMountItem`):**
  - Substituição completa do fallback de Cute Fishron pela identidade visual do Dragão do Cultista (Phantasm Dragon).
  - Textura dedicada `Content/Mounts/DragonMount.png` carregada via `ModContent.Request<Texture2D>` com folha vertical de 8 frames (120×640 px).
  - Ciclo de animação harmonizado para repouso, corrida no solo, voo contínuo e flutuação/manobras aéreas sem risco de divisão por zero.
  - Efeitos combinados de partículas carmesim de tempestade e centelhas etéreas cianas celestiais (`DustID.CrimsonTorch` e `DustID.Vortex`).
  - Criação do item de invocação `DragonMountItem` sem receita comercial ou de forja.
  - Adição do drop da montaria ao novo boss final (`ThornStormBoss`) utilizando `ModifyNPCLoot` e `ItemDropRule.Common(ModContent.ItemType<DragonMountItem>())`.
  - Ícone de buff exclusivo `DragonMountBuff.png` (32×32) e padronização das localizações pt-BR e en-US.

### Corrigido
- **Crash ao Usar a Montaria (`DragonMount` / `DivideByZeroException`):**
  - Corrigido o crash do motor (`System.DivideByZeroException` em `Terraria.Mount.Draw`) disparado ao ativar a montaria com a Adaga de Espinhos (`DragonPower`).
  - Causa raiz: `MountData.totalFrames` não era definido na inicialização, permanecendo em `0` e provocando divisão por zero no cálculo da altura de quadro (`textureHeight / totalFrames`).
  - Inicialização explícita e segura de `MountData.totalFrames` (23 quadros), `MountData.playerYOffsets`, offsets de jogador e dimensões de textura padrão, além de vinculação direta com fallback de texturas do `CuteFishron` em `SetStaticDefaults` e `SetMount`.

---

## [0.2.0] - 2026-09-22

### Adicionado
- **Proteção do Guarda-chuva Vanilla (`ItemID.Umbrella`):**
  - O Guarda-chuva vanilla bloqueia completamente o dano de espinhos enquanto estiver empunhado e aberto.
  - Sistema de durabilidade baseado em tempo de exposição contínua (1 ponto perdido a cada 0,5s de exposição com espinhos ativos).
  - Estados de conservação conceituais: *Novo* (100%–50%), *Danificado* (49%–15%), *Quase Quebrado* (14%–1%) e *Quebrado* (0%).
  - Barra visual de durabilidade renderizada diretamente no slot do inventário (`PostDrawInInventory`).
  - Tooltips detalhados indicando porcentagem de durabilidade e estado de conservação do item.
- **Imunidade da Poção Pele de Ferro (`BuffID.Ironskin`):**
  - Prioridade 1 de proteção: confere imunidade total ao dano dos espinhos enquanto o buff estiver ativo.
  - Preserva a durabilidade do Guarda-chuva empunhado durante a vigência do buff.
- **Drop da Geleia com Guarda-chuva (`NPCID.UmbrellaSlime`):**
  - 5% de chance (1 em 20) de dropar o Guarda-chuva vanilla ao ser derrotada, com autoridade no servidor.
- **Escalonamento Progressivo de Dano:**
  - Dano dinâmico conforme a progressão do mundo: Pré-Hardmode, Hardmode, Pós-Plantera, Pós-Golem e Pós-Moon Lord, adaptando-se aos modos Normal, Expert e Master.

### Modificado
- **Otimização de Performance e Spawns:**
  - Pacing de ciclo de spawn a cada 3 ticks.
  - Limite global de segurança elevado para 120 espinhos simultâneos no mundo.
  - Limite local por jogador estabelecido em 70 espinhos em um raio de 1500 unidades.
  - Tornou `IsPlayerInRainZone` público para reaproveitamento limpo entre sistemas sem duplicação.
- **Arquitetura Modular:**
  - Separação de responsabilidades em `Common/GlobalItems/`, `Common/GlobalNPCs/`, `Common/Players/` e `Common/Systems/`.

---

## [0.1.0] - 2026-09-21

### Adicionado
- Lançamento inicial do mod **O2 Thorn Rain**.
- Sistema de tempestade climática com espinhos perigosos caindo dos céus durante chuva.
- Trajetória com física dinâmica influenciada pela velocidade e direção do vento em tempo real (`Main.windSpeedCurrent`).
- Restrição de zona de risco para a superfície e céu (`ZoneOverworldHeight`, `ZoneSkyHeight`), resguardando o subterrâneo e cavernas.
- Projétil hostil customizado `SpikesProjectile` com partículas e áudio de impacto.
- Compatibilidade com multiplayer (autoridade de spawn no servidor).
