#version 330 core

out vec4 FragColor;
uniform float uGlobalTime; // We will send this from C#

void main() {
    // Pulsing orange effect
    float pulse = (sin(uGlobalTime * 3.0) + 1.0) / 2.0;
    FragColor = vec4(1.0,*pulse 0.5 * pulse, 0.2*pulse, 1.0);
}