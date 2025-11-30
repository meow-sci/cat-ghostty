# inspect a wasm file for symbols/exports

```bash
node -e "const fs=require('fs');const buf=fs.readFileSync('libghostty-ts/lib/ghostty-vt.wasm');const mod=new WebAssembly.Module(buf);console.log('Exports:',WebAssembly.Module.exports(mod));console.log('Imports:',WebAssembly.Module.imports(mod));"
```

## result:

```javascript
let exports = [                                                          
  { name: 'memory', kind: 'memory' },                               
  { name: 'ghostty_key_event_new', kind: 'function' },              
  { name: 'ghostty_key_event_free', kind: 'function' },             
  { name: 'ghostty_key_event_set_action', kind: 'function' },       
  { name: 'ghostty_key_event_get_action', kind: 'function' },       
  { name: 'ghostty_key_event_set_key', kind: 'function' },          
  { name: 'ghostty_key_event_get_key', kind: 'function' },          
  { name: 'ghostty_key_event_set_mods', kind: 'function' },         
  { name: 'ghostty_key_event_get_mods', kind: 'function' },         
  { name: 'ghostty_key_event_set_consumed_mods', kind: 'function' },
  { name: 'ghostty_key_event_get_consumed_mods', kind: 'function' },
  { name: 'ghostty_key_event_set_composing', kind: 'function' },    
  { name: 'ghostty_key_event_get_composing', kind: 'function' },    
  { name: 'ghostty_key_event_set_utf8', kind: 'function' },         
  { name: 'ghostty_key_event_get_utf8', kind: 'function' },         
  {                                                                 
    name: 'ghostty_key_event_set_unshifted_codepoint',              
    kind: 'function'                                                
  },                                                                
  {                                                                 
    name: 'ghostty_key_event_get_unshifted_codepoint',              
    kind: 'function'                                                
  },                                                                
  { name: 'ghostty_key_encoder_new', kind: 'function' },            
  { name: 'ghostty_key_encoder_free', kind: 'function' },           
  { name: 'ghostty_key_encoder_setopt', kind: 'function' },         
  { name: 'ghostty_key_encoder_encode', kind: 'function' },         
  { name: 'ghostty_osc_new', kind: 'function' },                    
  { name: 'ghostty_osc_free', kind: 'function' },                   
  { name: 'ghostty_osc_next', kind: 'function' },                   
  { name: 'ghostty_osc_reset', kind: 'function' },                  
  { name: 'ghostty_osc_end', kind: 'function' },                    
  { name: 'ghostty_osc_command_type', kind: 'function' },           
  { name: 'ghostty_osc_command_data', kind: 'function' },           
  { name: 'ghostty_paste_is_safe', kind: 'function' },              
  { name: 'ghostty_color_rgb_get', kind: 'function' },              
  { name: 'ghostty_sgr_new', kind: 'function' },                    
  { name: 'ghostty_sgr_free', kind: 'function' },                   
  { name: 'ghostty_sgr_reset', kind: 'function' },                  
  { name: 'ghostty_sgr_set_params', kind: 'function' },             
  { name: 'ghostty_sgr_next', kind: 'function' },                   
  { name: 'ghostty_sgr_unknown_full', kind: 'function' },           
  { name: 'ghostty_sgr_unknown_partial', kind: 'function' },        
  { name: 'ghostty_sgr_attribute_tag', kind: 'function' },          
  { name: 'ghostty_sgr_attribute_value', kind: 'function' },        
  { name: 'ghostty_wasm_alloc_opaque', kind: 'function' },          
  { name: 'ghostty_wasm_free_opaque', kind: 'function' },           
  { name: 'ghostty_wasm_alloc_u8_array', kind: 'function' },        
  { name: 'ghostty_wasm_free_u8_array', kind: 'function' },         
  { name: 'ghostty_wasm_alloc_u16_array', kind: 'function' },       
  { name: 'ghostty_wasm_free_u16_array', kind: 'function' },        
  { name: 'ghostty_wasm_alloc_u8', kind: 'function' },              
  { name: 'ghostty_wasm_free_u8', kind: 'function' },               
  { name: 'ghostty_wasm_alloc_usize', kind: 'function' },           
  { name: 'ghostty_wasm_free_usize', kind: 'function' },            
  { name: 'ghostty_wasm_alloc_sgr_attribute', kind: 'function' },   
  { name: 'ghostty_wasm_free_sgr_attribute', kind: 'function' }     
]                                                                   
let imports = [ { module: 'env', name: 'log', kind: 'function' } ]       

```