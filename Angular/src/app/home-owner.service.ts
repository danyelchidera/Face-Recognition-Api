import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from "@angular/common/http";

@Injectable({
  providedIn: 'root'
})
export class HomeOwnerService {

  constructor(private http: HttpClient) { }
  LoginForm={
    UserName:'',
    Password:'',
    grant_type:'password'
  }


  url = "http://localhost:65052/"
  data:Object;

  Login(){
   let data = "userName=" + this.LoginForm.UserName +"&password="+ this.LoginForm.Password + "&grant_type=password";
    console.log(data);
    return this.http.post("api/token", data)
  }
  getFace(): any{
    var token = localStorage.getItem('accessToken')
    //var token = localStorage.removeItem('accessToken')
    let tokenHeader = new HttpHeaders();
    tokenHeader = tokenHeader.set('Authorization', 'Bearer ' + token);
    //console.log(JSON.parse(token));
    console.log(23);


    return this.http.get("api/api/face", {headers : tokenHeader});
  }
  postUnknown(nameType:any):any{
    var token = localStorage.getItem('accessToken')
    var tokenHeader = new HttpHeaders().set('Authorization', 'Bearer '+ token);
    return this.http.post("api/api/insert/face",nameType, {headers : tokenHeader});
  }
  
}
