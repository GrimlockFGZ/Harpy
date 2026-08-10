#version 330 core
in vec3  vNormal;
flat in uint vBlockId;
in float vLight;
out vec4 FragColor;

const vec3 uLightDir = normalize(vec3(0.6, 1.0, 0.4));

vec3 blockColour(uint id)
{
    if (id == 1u) return vec3(0.50, 0.50, 0.50);
    if (id == 2u) return vec3(0.55, 0.37, 0.22);
    if (id == 3u) return vec3(0.30, 0.65, 0.20);
    if (id == 4u) return vec3(0.85, 0.80, 0.55);
    if (id == 5u) return vec3(0.45, 0.30, 0.15);
    if (id == 6u) return vec3(0.20, 0.55, 0.15);
    return vec3(1.0, 0.0, 1.0);
}

void main()
{
    vec3 base     = blockColour(vBlockId);
    float diff    = max(dot(normalize(vNormal), uLightDir), 0.0);
    float ambient = 0.25;
    vec3 colour   = base * (ambient + (1.0 - ambient) * diff) * vLight;
    FragColor     = vec4(colour, 1.0);
}