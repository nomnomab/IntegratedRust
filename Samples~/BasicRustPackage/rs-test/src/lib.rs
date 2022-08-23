#[repr(C)]
pub struct Vector2 {
    pub x: f32,
    pub y: f32
}

#[repr(C)]
pub struct Color32 {
    pub r: u8,
    pub g: u8,
    pub b: u8,
    pub a: u8
}

#[no_mangle]
pub extern "C" fn get_position(time: f32, amplitude: f32, frequency: f32) -> Vector2 {
    let x = (time * frequency).cos() * amplitude;
    let y = (time * frequency).sin() * amplitude;

    Vector2 { x, y }
}

#[no_mangle]
pub extern "C" fn get_color() -> Color32 {
    Color32 {
        r: 45,
        g: 45,
        b: 45,
        a: 255
    }
}