# Rosaure ECS

-Custom ECS written in C# for Unity<br>
-WIP<br>
-Next Performance Update : Use GPU Instancing + Custom physic to replace Unity's GameObjects<br>

## What's an ECS?

-[ECS](https://en.wikipedia.org/wiki/Entity_component_system) stands for Entity Component System<br>
-Allow to manage a lot of Entities in a Simulation<br>
-e.g. Enemies & Projectiles in a Game<br>

## Goals

-Max performance<br>
-Engine agnostic<br>
-Modularity<br>

## Features

-Base ECS that can be implemented by other Systems<br>
-Sub Array<br>
-Define Entity Types per System implementing Rosaure (e.g. Enemy1, Enemy2)<br>
-Custom data per Entity Type<br>
-Custom behaviors/logic per Entity Type<br>

## Battleship Vs Space Armada

[Web Version]()<br>
[Download in release section]()<br>
-Technical Demo game for Rosaure ECS<br>
-Implement Rosaure in EnemyManager and ProjectileManager<br>