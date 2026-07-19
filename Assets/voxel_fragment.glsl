#version 330 core

in vec3  vNormal;
in float vBlockId;

out vec4 FragColor;

// Simple directional light from above-right
const vec3 uLightDir = normalize(vec3(0.6, 1.0, 0.4));

// Per-block base colours (index matches BlockType enum)
vec3 blockColour(int id)
{
    if (id == 1) return vec3(0.50, 0.50, 0.50); // Stone
    if (id == 2) return vec3(0.55, 0.37, 0.22); // Dirt
    if (id == 3) return vec3(0.30, 0.65, 0.20); // Grass
    if (id == 4) return vec3(0.85, 0.80, 0.55); // Sand
    if (id == 5) return vec3(0.45, 0.30, 0.15); // Wood
    if (id == 6) return vec3(0.20, 0.55, 0.15); // Leaf
    return vec3(1.0, 0.0, 1.0);                 // Unknown — magenta
}

void main()
{
    vec3 base    = blockColour(int(vBlockId + 0.5));
    float diff   = max(dot(normalize(vNormal), uLightDir), 0.0);
    float ambient = 0.25;
    vec3 colour  = base * (ambient + (1.0 - ambient) * diff);
    FragColor    = vec4(colour, 1.0);
}
