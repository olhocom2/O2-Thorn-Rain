# Changelog

Todas as alterações notáveis neste projeto serão documentadas neste arquivo.

O formato é baseado no [Keep a Changelog](https://keepachangelog.com/pt-BR/1.0.0/)
e este projeto adere ao [Semantic Versioning](https://semver.org/lang/pt-BR/).

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
