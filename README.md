# The Last Order ⚔️
The Last Order es un juego de aventura y exploración desarrollado en Unity, fuertemente inspirado en clásicos de la era de 16/32 bits como The Legend of Zelda: The Minish Cap.
Este proyecto es un laboratorio de simulación donde el comportamiento del mundo y sus habitantes está regido por modelos matemáticos y algoritmos de aleatoriedad (RNG) para crear una experiencia dinámica y menos predecible.

## Características Principales
Estética Retro: Gráficos en pixel art con perspectiva top-down.
Sistemas de Simulación: Implementación de modelos lógicos para el comportamiento de NPCs y eventos.
Motor: Desarrollado íntegramente en Unity 2D.
RNG Avanzado: Sistema de generación de números aleatorios para la variabilidad de drops y encuentros.

## Modelos de Simulación Implementados
El núcleo de The Last Order reside en cómo utiliza la simulación para dar vida al reino. Hemos implementado cuatro modelos clave:

1. Caminata Pseudoaleatoria (Random Walk)
Utilizado principalmente para el movimiento ambiental de NPCs. En lugar de rutas fijas, los entes deciden su siguiente paso basándose en probabilidades, generando un comportamiento de exploración más natural y menos robótico.
2. Sistema de Agentes
Los enemigos y NPCs interactivos operan bajo un modelo de agentes autónomos. Cada agente percibe su entorno y toma decisiones (atacar, patrullar) basándose en estados internos y objetivos específicos, permitiendo comportamientos emergentes.
3. Cadenas de Markov
Aplicado al clima y ciclos de eventos. El estado actual del mundo (ej. "Soleado") influye en la probabilidad del siguiente estado, asegurando que los cambios en el entorno tengan una progresión lógica y no totalmente errática.
4. Teoría de Colas (Queuing Theory)
Implementado en la logística de salas trampa. Los NPCs forman filas y gestionan tiempos de espera para interactuar con el jugador, evitando aglomeraciones caóticas.

## Tecnologías Utilizadas
* Engine: Unity 2022.x / 2023.x
* Lenguaje: C#
* Control de Versiones: Git
* Matemáticas: Implementaciones personalizadas de RNG y matrices de transición para Markov.

## Clonar el repositorio:
   ```bash
   git clone [https://github.com/Hellscythe26/TheLastOrder.git](https://github.com/tu-usuario/TheLastOrder.git)
