import { Component, OnInit } from '@angular/core';
import { NgForm } from '@angular/forms';
import { HomeOwnerService } from '../home-owner.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css']
})
export class LoginComponent implements OnInit {

  constructor(public service: HomeOwnerService, private router: Router) { }

 
  ngOnInit(): void {
    if(sessionStorage.getItem('accessToken') != null){
      this.router.navigateByUrl('/dashboard'); 
    }
  }
  onSubmit(){
    this.service.Login().subscribe(
      (res:any)=>{
        console.log(res.accessToken);
        localStorage.setItem('accessToken', res.access_token);
        this.router.navigateByUrl('/dashboard'); 
      },
      err=>{
        console.log(err);
      }
    )
  }

}
