#version 330 core

out vec4 FragColor;
in vec2 v_vTexcoord;

uniform float uTime;

vec3 hsv2rgb(vec3 c) {
    vec4 K = vec4(1.0, 2.0/3.0, 1.0/3.0, 3.0);
    vec3 p = abs(fract(c.xxx + K.xyz) * 6.0 - K.www);
    return c.z * mix(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
}

void main() {

    float hue = fract(v_vTexcoord.x + uTime * 0.7);
    vec3 col = hsv2rgb(vec3(hue, 0.9, 1.0));

    FragColor = vec4(col, 1.0);
}