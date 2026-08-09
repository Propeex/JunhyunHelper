# MAP UPDATE POLICY SUMMARY

상태: `CONFIRMED`

준현 헬퍼의 지도도 Quest/Hideout/Ammo와 같은 **콘텐츠 업데이트 대상**으로 관리합니다.

- gameplay/location data: `json.tarkov.dev/<game-mode>/maps`
- layout metadata: Tarkov.dev public map configuration
- background asset: CC BY-NC-SA 4.0 조건을 준수하는 layered SVG map source

사용자는 준현 헬퍼를 비상업적 플레이 보조 도구로 운영하고 필요한 attribution 조건을 수용합니다.

정상적인 데이터/좌표/asset 변화는 importer가 자동 흡수합니다. 비호환 schema, 알 수 없는 좌표계, 깨진 layer/asset, 불명확한 license provenance는 추측하지 않고 마지막 정상 지도를 보호합니다.

상세 설계: `docs/MAP_PRODUCT_DECISION.md`, `docs/MAP_DATA_SOURCE_ANALYSIS.md`
