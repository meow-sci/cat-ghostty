export interface KeyEvent {
  _type: "KeyEvent";
  // corrosponds to DOM KeyboardEvent.code
  code: string;
  // corrosponds to DOM KeyboardEvent.key
  key: string;
  shiftKey: boolean;
  altKey: boolean;
  metaKey: boolean;
  ctrlKey: boolean;
}
