# Changelog

Todas as alterações notáveis neste projeto serão documentadas neste arquivo.

O formato é baseado no [Keep a Changelog](https://keepachangelog.com/pt-BR/1.0.0/)
e este projeto adere ao [Semantic Versioning](https://semver.org/lang/pt-BR/).

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
